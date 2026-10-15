BEGIN;

TRUNCATE TABLE reservations, loans, book_copies, book_genres, book_authors, books, genres, authors, users RESTART IDENTITY CASCADE;

-- 1. РОЗШИРЕННЯ
CREATE EXTENSION IF NOT EXISTS pg_trgm;   

-- 2. ДОМЕНИ (якщо ще не створені, CREATE TABLE IF NOT EXISTS/DO)

DO $$ BEGIN
    IF NOT EXISTS (SELECT 1 FROM pg_type WHERE typname = 'email_address') THEN
        CREATE DOMAIN email_address AS VARCHAR(255) CHECK (VALUE ~* '^[A-Za-z0-9._%+-]+@[A-Za-z0-9.-]+\.[A-Za-z]{2,}$');
    END IF;
    IF NOT EXISTS (SELECT 1 FROM pg_type WHERE typname = 'person_name') THEN
        CREATE DOMAIN person_name AS VARCHAR(150) CHECK (length(btrim(VALUE)) > 0);
    END IF;
    IF NOT EXISTS (SELECT 1 FROM pg_type WHERE typname = 'publication_year') THEN
        CREATE DOMAIN publication_year AS SMALLINT CHECK (VALUE BETWEEN 1000 AND 2100);
    END IF;
    IF NOT EXISTS (SELECT 1 FROM pg_type WHERE typname = 'isbn_code') THEN
        CREATE DOMAIN isbn_code AS VARCHAR(20) CHECK (regexp_replace(VALUE, '[-\s]', '', 'g') ~ '^(\d{9}[\dXx]|\d{13})$');
    END IF;
    IF NOT EXISTS (SELECT 1 FROM pg_type WHERE typname = 'positive_int') THEN
        CREATE DOMAIN positive_int AS INTEGER CHECK (VALUE > 0);
    END IF;
END $$;

-- 3. ПЕРЕЛІКИ (ENUM)
DO $$ BEGIN
    IF NOT EXISTS (SELECT 1 FROM pg_type WHERE typname = 'user_role') THEN
        CREATE TYPE user_role AS ENUM ('reader', 'librarian', 'admin');
    END IF;
    IF NOT EXISTS (SELECT 1 FROM pg_type WHERE typname = 'copy_status') THEN
        CREATE TYPE copy_status AS ENUM ('available', 'on_loan', 'reserved', 'written_off');
    END IF;
    IF NOT EXISTS (SELECT 1 FROM pg_type WHERE typname = 'reservation_status') THEN
        CREATE TYPE reservation_status AS ENUM ('active', 'fulfilled', 'cancelled', 'expired');
    END IF;
END $$;

-- 4. СТВОРЕННЯ ТАБЛИЦЬ (якщо вони вже є, пропускаємо або перестворюємо)
CREATE TABLE IF NOT EXISTS users (
    user_id       BIGINT GENERATED ALWAYS AS IDENTITY PRIMARY KEY,
    full_name     person_name   NOT NULL,
    email         email_address NOT NULL,
    password_hash TEXT          NOT NULL,
    role          user_role     NOT NULL DEFAULT 'reader',
    is_active     BOOLEAN       NOT NULL DEFAULT TRUE,
    created_at    TIMESTAMPTZ   NOT NULL DEFAULT now()
);
CREATE UNIQUE INDEX IF NOT EXISTS ux_users_email ON users (lower(email));

CREATE TABLE IF NOT EXISTS authors (
    author_id   BIGINT GENERATED ALWAYS AS IDENTITY PRIMARY KEY,
    full_name   person_name NOT NULL,
    birth_year  publication_year,
    country     VARCHAR(100),
    CONSTRAINT uq_author UNIQUE (full_name, birth_year)
);

CREATE TABLE IF NOT EXISTS genres (
    genre_id  INTEGER GENERATED ALWAYS AS IDENTITY PRIMARY KEY,
    name      VARCHAR(100) NOT NULL UNIQUE
);

CREATE TABLE IF NOT EXISTS books (
    book_id            BIGINT GENERATED ALWAYS AS IDENTITY PRIMARY KEY,
    title              VARCHAR(300)     NOT NULL CHECK (length(btrim(title)) > 0),
    isbn               isbn_code        UNIQUE,
    publication_year   publication_year NOT NULL,
    publisher          VARCHAR(200),
    description        TEXT,
    created_at         TIMESTAMPTZ      NOT NULL DEFAULT now()
);

CREATE TABLE IF NOT EXISTS book_authors (
    book_id    BIGINT NOT NULL REFERENCES books(book_id)     ON DELETE CASCADE,
    author_id  BIGINT NOT NULL REFERENCES authors(author_id) ON DELETE RESTRICT,
    PRIMARY KEY (book_id, author_id)
);

CREATE TABLE IF NOT EXISTS book_genres (
    book_id   BIGINT  NOT NULL REFERENCES books(book_id)   ON DELETE CASCADE,
    genre_id  INTEGER NOT NULL REFERENCES genres(genre_id) ON DELETE RESTRICT,
    PRIMARY KEY (book_id, genre_id)
);

CREATE TABLE IF NOT EXISTS book_copies (
    copy_id            BIGINT GENERATED ALWAYS AS IDENTITY PRIMARY KEY,
    book_id            BIGINT      NOT NULL REFERENCES books(book_id) ON DELETE RESTRICT,
    inventory_number   VARCHAR(30) NOT NULL UNIQUE,
    status             copy_status NOT NULL DEFAULT 'available',
    acquired_at        DATE        NOT NULL DEFAULT CURRENT_DATE,
    written_off_at     DATE,
    write_off_reason   TEXT,
    CONSTRAINT ck_writeoff_consistent CHECK (
        (status = 'written_off' AND written_off_at IS NOT NULL)
        OR (status <> 'written_off' AND written_off_at IS NULL)
    )
);

CREATE TABLE IF NOT EXISTS loans (
    loan_id        BIGINT GENERATED ALWAYS AS IDENTITY PRIMARY KEY,
    copy_id        BIGINT      NOT NULL REFERENCES book_copies(copy_id) ON DELETE RESTRICT,
    reader_id      BIGINT      NOT NULL REFERENCES users(user_id)       ON DELETE RESTRICT,
    librarian_id   BIGINT      REFERENCES users(user_id)                ON DELETE SET NULL,
    loaned_at      TIMESTAMPTZ NOT NULL DEFAULT now(),
    due_date       DATE        NOT NULL,
    returned_at    TIMESTAMPTZ,
    CONSTRAINT ck_due_after_loan    CHECK (due_date >= loaned_at::date),
    CONSTRAINT ck_return_after_loan CHECK (returned_at IS NULL OR returned_at >= loaned_at)
);

CREATE UNIQUE INDEX IF NOT EXISTS ux_loans_one_active_per_copy ON loans (copy_id) WHERE returned_at IS NULL;

CREATE TABLE IF NOT EXISTS reservations (
    reservation_id   BIGINT GENERATED ALWAYS AS IDENTITY PRIMARY KEY,
    book_id          BIGINT             NOT NULL REFERENCES books(book_id) ON DELETE CASCADE,
    reader_id        BIGINT             NOT NULL REFERENCES users(user_id) ON DELETE CASCADE,
    reserved_at      TIMESTAMPTZ        NOT NULL DEFAULT now(),
    expires_at       TIMESTAMPTZ        NOT NULL,
    status           reservation_status NOT NULL DEFAULT 'active',
    CONSTRAINT ck_reservation_period CHECK (expires_at > reserved_at)
);

CREATE UNIQUE INDEX IF NOT EXISTS ux_reservation_active_per_reader_book
    ON reservations (reader_id, book_id) WHERE status = 'active';

-- 5. ІНДЕКСИ
CREATE INDEX IF NOT EXISTS ix_books_title_trgm   ON books   USING gin (title gin_trgm_ops);
CREATE INDEX IF NOT EXISTS ix_authors_name_trgm  ON authors USING gin (full_name gin_trgm_ops);
CREATE INDEX IF NOT EXISTS ix_books_year         ON books (publication_year);
CREATE INDEX IF NOT EXISTS ix_book_genres_genre  ON book_genres (genre_id);
CREATE INDEX IF NOT EXISTS ix_book_authors_auth  ON book_authors (author_id);
CREATE INDEX IF NOT EXISTS ix_copies_book_status ON book_copies (book_id, status);
CREATE INDEX IF NOT EXISTS ix_loans_reader       ON loans (reader_id);
CREATE INDEX IF NOT EXISTS ix_loans_loaned_at    ON loans (loaned_at);
CREATE INDEX IF NOT EXISTS ix_reservations_book  ON reservations (book_id, status);

-- 6. ТРИГЕРИ
CREATE OR REPLACE FUNCTION fn_loan_sync_copy_status() RETURNS trigger AS $$
BEGIN
    IF TG_OP = 'INSERT' THEN
        IF (SELECT status FROM book_copies WHERE copy_id = NEW.copy_id) = 'written_off' THEN
            RAISE EXCEPTION 'Примірник % списано, видача неможлива', NEW.copy_id;
        END IF;
        UPDATE book_copies SET status = 'on_loan' WHERE copy_id = NEW.copy_id;
    ELSIF TG_OP = 'UPDATE' AND OLD.returned_at IS NULL AND NEW.returned_at IS NOT NULL THEN
        UPDATE book_copies SET status = 'available'
        WHERE copy_id = NEW.copy_id AND status = 'on_loan';
    END IF;
    RETURN NEW;
END;
$$ LANGUAGE plpgsql;

DROP TRIGGER IF EXISTS trg_loans_sync_copy ON loans;
CREATE TRIGGER trg_loans_sync_copy
AFTER INSERT OR UPDATE OF returned_at ON loans
FOR EACH ROW EXECUTE FUNCTION fn_loan_sync_copy_status();

-- 7. ПРЕДСТАВЛЕННЯ (VIEWS)
CREATE OR REPLACE VIEW v_book_catalog AS
SELECT
    b.book_id,
    b.title,
    b.isbn,
    b.publication_year,
    b.publisher,
    COALESCE((SELECT string_agg(a.full_name, ', ' ORDER BY a.full_name)
              FROM book_authors ba JOIN authors a USING (author_id)
              WHERE ba.book_id = b.book_id), '') AS authors,
    COALESCE((SELECT string_agg(g.name, ', ' ORDER BY g.name)
              FROM book_genres bg JOIN genres g USING (genre_id)
              WHERE bg.book_id = b.book_id), '') AS genres,
    (SELECT count(*) FROM book_copies c
      WHERE c.book_id = b.book_id AND c.status <> 'written_off') AS total_copies,
    (SELECT count(*) FROM book_copies c
      WHERE c.book_id = b.book_id AND c.status = 'available')    AS available_copies
FROM books b;

CREATE OR REPLACE VIEW v_active_loans AS
SELECT l.loan_id, l.loaned_at, l.due_date,
       u.user_id AS reader_id, u.full_name AS reader_name, u.email AS reader_email,
       c.copy_id, c.inventory_number, b.book_id, b.title,
       GREATEST(CURRENT_DATE - l.due_date, 0) AS days_overdue
FROM loans l
JOIN users u        ON u.user_id = l.reader_id
JOIN book_copies c  ON c.copy_id = l.copy_id
JOIN books b        ON b.book_id = c.book_id
WHERE l.returned_at IS NULL;

CREATE OR REPLACE VIEW v_overdue_loans AS
SELECT * FROM v_active_loans WHERE days_overdue > 0;

CREATE OR REPLACE VIEW v_popular_books AS
SELECT b.book_id, b.title, count(l.loan_id) AS loans_count
FROM books b
LEFT JOIN book_copies c ON c.book_id = b.book_id
LEFT JOIN loans l       ON l.copy_id = c.copy_id
GROUP BY b.book_id, b.title
ORDER BY loans_count DESC, b.title;

CREATE OR REPLACE VIEW v_monthly_loans AS
SELECT date_trunc('month', loaned_at)::date AS month,
       count(*)                             AS loans_count,
       count(returned_at)                   AS returned_count
FROM loans
GROUP BY 1
ORDER BY 1;

CREATE OR REPLACE VIEW v_loans_by_genre AS
SELECT g.name AS genre, count(l.loan_id) AS loans_count
FROM genres g
LEFT JOIN book_genres bg ON bg.genre_id = g.genre_id
LEFT JOIN book_copies c  ON c.book_id = bg.book_id
LEFT JOIN loans l        ON l.copy_id = c.copy_id
GROUP BY g.name
ORDER BY loans_count DESC;

-- 8. ТЕСТОВІ ДАНІ ЗІ СКИНУТИМИ ІДЕНТИФІКАТОРАМИ
INSERT INTO users (full_name, email, password_hash, role) VALUES
('Олена Бондаренко', 'olena.b@gmail.com', '$2a$12$e83...hash1', 'reader'),
('Іван Ковальчук', 'ivan.k@gmail.com', '$2a$12$e83...hash2', 'reader'),
('Марія Ткаченко', 'maria.t@gmail.com', '$2a$12$e83...hash3', 'reader'),
('Оксана Мандзювата', 'o.mandzyuvata@smartlib.ua', '$2a$12$e83...hash4', 'librarian'),
('Софія Шелеп', 's.shelep@smartlib.ua', '$2a$12$e83...hash5', 'admin');

INSERT INTO authors (full_name, birth_year, country) VALUES
('Тарас Шевченко', 1814, 'Україна'),
('Іван Франко', 1856, 'Україна'),
('Джордж Орвелл', 1903, 'Велика Британія'),
('Дж. К. Роулінг', 1965, 'Велика Британія'),
('Франц Кафка', 1883, 'Австро-Угорщина');

INSERT INTO genres (name) VALUES
('Класична література'),
('Поезія'),
('Антиутопія'),
('Фентезі'),
('Психологічна проза');

INSERT INTO books (title, isbn, publication_year, publisher, description) VALUES
('Кобзар', '978-966-03-8001-1', 2020, 'Фоліо', 'Збірка поетичних творів Тараса Шевченка.'),
('Захар Беркут', '978-966-03-8002-8', 2019, 'Школа', 'Історична повість про боротьбу общини проти монгольської навали.'),
('1984', '978-617-7538-01-0', 2021, 'Видавництво Старого Лева', 'Роман-антиутопія про тоталітарне суспільство.'),
('Гаррі Поттер і філософський камінь', '978-617-585-047-3', 2022, 'А-БА-БА-ГА-ЛА-МА-ГА', 'Перша книга про пригоди юного чарівника.'),
('Перевтілення', '978-966-03-8003-5', 2018, 'Фоліо', 'Новела про відчуження людини в сучасному світі.');

-- 5. ЗВ'ЯЗКИ КНИГА <-> АВТОР (багато-до-багатьох)
INSERT INTO book_authors (book_id, author_id) VALUES
(1, 1), -- Кобзар -> Тарас Шевченко
(2, 2), -- Захар Беркут -> Іван Франко
(3, 3), -- 1984 -> Джордж Орвелл
(4, 4), -- Гаррі Поттер і філософський камінь -> Дж. К. Роулінг
(5, 5); -- Перевтілення -> Франц Кафка

-- 6. ЗВ'ЯЗКИ КНИГА <-> ЖАНР (багато-до-багатьох)
INSERT INTO book_genres (book_id, genre_id) VALUES
(1, 1), (1, 2), -- Кобзар -> Класична література, Поезія
(2, 1),          -- Захар Беркут -> Класична література
(3, 3),          -- 1984 -> Антиутопія
(4, 4),          -- Гаррі Поттер і філософський камінь -> Фентезі
(5, 1), (5, 5); -- Перевтілення -> Класична література, Психологічна проза

INSERT INTO book_copies (book_id, inventory_number, status, written_off_at, write_off_reason) VALUES
(1, 'INV-00101', 'available', NULL, NULL),
(1, 'INV-00102', 'available', NULL, NULL),
(2, 'INV-00201', 'available', NULL, NULL),
(3, 'INV-00301', 'available', NULL, NULL),
(3, 'INV-00302', 'available', NULL, NULL),
(4, 'INV-00401', 'available', NULL, NULL),
(5, 'INV-00501', 'written_off', CURRENT_DATE, 'Пошкодження сторінок');

INSERT INTO loans (copy_id, reader_id, librarian_id, loaned_at, due_date, returned_at) VALUES
(1, 1, 4, NOW() - INTERVAL '30 days', CURRENT_DATE - INTERVAL '16 days', NOW() - INTERVAL '15 days');

INSERT INTO loans (copy_id, reader_id, librarian_id, loaned_at, due_date) VALUES
(3, 1, 4, NOW() - INTERVAL '5 days', CURRENT_DATE + INTERVAL '9 days');

INSERT INTO loans (copy_id, reader_id, librarian_id, loaned_at, due_date) VALUES
(4, 2, 4, NOW() - INTERVAL '20 days', CURRENT_DATE - INTERVAL '6 days');

INSERT INTO reservations (book_id, reader_id, reserved_at, expires_at, status) VALUES
(2, 3, NOW(), NOW() + INTERVAL '3 days', 'active');

COMMIT;