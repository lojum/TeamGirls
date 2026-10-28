BEGIN;

-- 1. РОЗШИРЕННЯ
CREATE EXTENSION IF NOT EXISTS pg_trgm;   

-- 2. ДОМЕНИ
DO $$ BEGIN
    IF NOT EXISTS (SELECT 1 FROM pg_type WHERE typname = 'email_address') THEN
        CREATE DOMAIN email_address AS VARCHAR(255)
            CHECK (VALUE ~* '^[A-Za-z0-9._%+-]+@[A-Za-z0-9.-]+\.[A-Za-z]{2,}$');
    END IF;
    IF NOT EXISTS (SELECT 1 FROM pg_type WHERE typname = 'person_name') THEN
        CREATE DOMAIN person_name AS VARCHAR(150)
            CHECK (length(btrim(VALUE)) > 0);
    END IF;
    IF NOT EXISTS (SELECT 1 FROM pg_type WHERE typname = 'publication_year') THEN
        CREATE DOMAIN publication_year AS SMALLINT
            CHECK (VALUE BETWEEN 1000 AND 2100);
    END IF;
    IF NOT EXISTS (SELECT 1 FROM pg_type WHERE typname = 'birth_year_t') THEN
        CREATE DOMAIN birth_year_t AS SMALLINT
            CHECK (VALUE BETWEEN 1 AND 2100);
    END IF;
    IF NOT EXISTS (SELECT 1 FROM pg_type WHERE typname = 'isbn_code') THEN
        CREATE DOMAIN isbn_code AS VARCHAR(20)
            CHECK (regexp_replace(VALUE, '[-\s]', '', 'g') ~ '^(\d{9}[\dXx]|\d{13})$');
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

-- 4. ТАБЛИЦІ (наявні пропускаються, їх доповнює розділ 5)
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
    full_name   person_name  NOT NULL,
    birth_year  birth_year_t,
    country     VARCHAR(100),
    CONSTRAINT uq_author UNIQUE (full_name, birth_year)
);

CREATE TABLE IF NOT EXISTS genres (
    genre_id  INTEGER GENERATED ALWAYS AS IDENTITY PRIMARY KEY,
    name      VARCHAR(100) NOT NULL UNIQUE
);

CREATE TABLE IF NOT EXISTS locations (
    location_id INTEGER GENERATED ALWAYS AS IDENTITY PRIMARY KEY,
    room        VARCHAR(100) NOT NULL,
    shelf       VARCHAR(50)  NOT NULL,
    CONSTRAINT uq_location UNIQUE (room, shelf)
);

CREATE TABLE IF NOT EXISTS books (
    book_id          BIGINT GENERATED ALWAYS AS IDENTITY PRIMARY KEY,
    title            VARCHAR(300)     NOT NULL CHECK (length(btrim(title)) > 0),
    isbn             isbn_code        UNIQUE,
    publication_year publication_year NOT NULL,
    publisher        VARCHAR(200),
    description      TEXT,
    language         VARCHAR(10)      NOT NULL DEFAULT 'uk',
    cover_url        TEXT,
    created_at       TIMESTAMPTZ      NOT NULL DEFAULT now()
);

CREATE TABLE IF NOT EXISTS book_authors (
    book_id   BIGINT NOT NULL REFERENCES books(book_id)     ON DELETE CASCADE,
    author_id BIGINT NOT NULL REFERENCES authors(author_id) ON DELETE RESTRICT,
    PRIMARY KEY (book_id, author_id)
);

CREATE TABLE IF NOT EXISTS book_genres (
    book_id  BIGINT  NOT NULL REFERENCES books(book_id)   ON DELETE CASCADE,
    genre_id INTEGER NOT NULL REFERENCES genres(genre_id) ON DELETE RESTRICT,
    PRIMARY KEY (book_id, genre_id)
);

CREATE TABLE IF NOT EXISTS book_copies (
    copy_id          BIGINT GENERATED ALWAYS AS IDENTITY PRIMARY KEY,
    book_id          BIGINT      NOT NULL REFERENCES books(book_id)        ON DELETE RESTRICT,
    location_id      INTEGER     REFERENCES locations(location_id)         ON DELETE SET NULL,
    inventory_number VARCHAR(30) NOT NULL UNIQUE,
    status           copy_status NOT NULL DEFAULT 'available',
    acquired_at      DATE        NOT NULL DEFAULT CURRENT_DATE,
    written_off_at   DATE,
    write_off_reason TEXT,
    CONSTRAINT ck_writeoff_consistent CHECK (
        (status = 'written_off' AND written_off_at IS NOT NULL)
        OR (status <> 'written_off' AND written_off_at IS NULL)
    )
);

CREATE TABLE IF NOT EXISTS loans (
    loan_id       BIGINT GENERATED ALWAYS AS IDENTITY PRIMARY KEY,
    copy_id       BIGINT      NOT NULL REFERENCES book_copies(copy_id) ON DELETE RESTRICT,
    reader_id     BIGINT      NOT NULL REFERENCES users(user_id)       ON DELETE RESTRICT,
    librarian_id  BIGINT      REFERENCES users(user_id)                ON DELETE SET NULL,
    loaned_at     TIMESTAMPTZ NOT NULL DEFAULT now(),
    due_date      DATE        NOT NULL,
    returned_at   TIMESTAMPTZ,
    renewal_count SMALLINT    NOT NULL DEFAULT 0,
    CONSTRAINT ck_due_after_loan    CHECK (due_date >= loaned_at::date),
    CONSTRAINT ck_return_after_loan CHECK (returned_at IS NULL OR returned_at >= loaned_at)
);

CREATE TABLE IF NOT EXISTS fines (
    fine_id    BIGINT GENERATED ALWAYS AS IDENTITY PRIMARY KEY,
    loan_id    BIGINT         NOT NULL REFERENCES loans(loan_id) ON DELETE CASCADE,
    amount     NUMERIC(10, 2) NOT NULL CHECK (amount > 0),
    is_paid    BOOLEAN        NOT NULL DEFAULT FALSE,
    created_at TIMESTAMPTZ    NOT NULL DEFAULT now(),
    paid_at    TIMESTAMPTZ,
    CONSTRAINT ck_paid_timestamp CHECK (
        (is_paid = TRUE AND paid_at IS NOT NULL) OR (is_paid = FALSE AND paid_at IS NULL)
    )
);

CREATE TABLE IF NOT EXISTS reservations (
    reservation_id BIGINT GENERATED ALWAYS AS IDENTITY PRIMARY KEY,
    book_id        BIGINT             NOT NULL REFERENCES books(book_id) ON DELETE CASCADE,
    reader_id      BIGINT             NOT NULL REFERENCES users(user_id) ON DELETE CASCADE,
    reserved_at    TIMESTAMPTZ        NOT NULL DEFAULT now(),
    expires_at     TIMESTAMPTZ        NOT NULL,
    status         reservation_status NOT NULL DEFAULT 'active',
    CONSTRAINT ck_reservation_period CHECK (expires_at > reserved_at)
);

-- 5. ДОПОВНЕННЯ НАЯВНИХ ТАБЛИЦЬ ДО НОВОЇ СХЕМИ
ALTER TABLE books
    ADD COLUMN IF NOT EXISTS language  VARCHAR(10) NOT NULL DEFAULT 'uk',
    ADD COLUMN IF NOT EXISTS cover_url TEXT;

ALTER TABLE book_copies
    ADD COLUMN IF NOT EXISTS location_id INTEGER REFERENCES locations(location_id) ON DELETE SET NULL;

ALTER TABLE loans
    ADD COLUMN IF NOT EXISTS renewal_count SMALLINT NOT NULL DEFAULT 0;

DO $$ BEGIN
    IF NOT EXISTS (SELECT 1 FROM pg_constraint WHERE conname = 'ck_renewal_count') THEN
        ALTER TABLE loans ADD CONSTRAINT ck_renewal_count CHECK (renewal_count BETWEEN 0 AND 2);
    END IF;
END $$;

-- Рік народження автора: окремий домен замість publication_year
DO $$ BEGIN
    IF (SELECT domain_name FROM information_schema.columns
        WHERE table_schema = current_schema()
          AND table_name = 'authors' AND column_name = 'birth_year') IS DISTINCT FROM 'birth_year_t' THEN
        ALTER TABLE authors ALTER COLUMN birth_year TYPE birth_year_t;
    END IF;
END $$;

-- 6. ІНДЕКСИ
-- Один активний (неповернений) запис видачі на примірник
CREATE UNIQUE INDEX IF NOT EXISTS ux_loans_one_active_per_copy
    ON loans (copy_id) WHERE returned_at IS NULL;
-- Одне активне резервування однієї книги на читача
CREATE UNIQUE INDEX IF NOT EXISTS ux_reservation_active_per_reader_book
    ON reservations (reader_id, book_id) WHERE status = 'active';

CREATE INDEX IF NOT EXISTS ix_books_title_trgm     ON books   USING gin (title gin_trgm_ops);
CREATE INDEX IF NOT EXISTS ix_authors_name_trgm    ON authors USING gin (full_name gin_trgm_ops);
CREATE INDEX IF NOT EXISTS ix_books_year           ON books (publication_year);
CREATE INDEX IF NOT EXISTS ix_book_genres_genre    ON book_genres (genre_id);
CREATE INDEX IF NOT EXISTS ix_book_authors_auth    ON book_authors (author_id);
CREATE INDEX IF NOT EXISTS ix_copies_book_status   ON book_copies (book_id, status);
CREATE INDEX IF NOT EXISTS ix_loans_reader         ON loans (reader_id);
CREATE INDEX IF NOT EXISTS ix_loans_loaned_at      ON loans (loaned_at);
CREATE INDEX IF NOT EXISTS ix_loans_active_reader  ON loans (reader_id) WHERE returned_at IS NULL;
CREATE INDEX IF NOT EXISTS ix_fines_loan           ON fines (loan_id);
CREATE INDEX IF NOT EXISTS ix_reservations_book    ON reservations (book_id, status);
CREATE INDEX IF NOT EXISTS ix_reservations_queue   ON reservations (book_id, reserved_at) WHERE status = 'active';

-- 7. ТРИГЕР синхронізації статусу примірника (copy_status) 
CREATE OR REPLACE FUNCTION fn_loan_sync_copy_status() RETURNS trigger AS $$
DECLARE
    v_status copy_status;
BEGIN
    IF TG_OP = 'INSERT' THEN
        IF NEW.returned_at IS NULL THEN
            SELECT status INTO v_status FROM book_copies WHERE copy_id = NEW.copy_id;

            IF v_status = 'written_off' THEN
                RAISE EXCEPTION 'Примірник % списано, видача неможлива', NEW.copy_id;
            ELSIF v_status = 'on_loan' THEN
                RAISE EXCEPTION 'Примірник % вже перебуває у видачі', NEW.copy_id;
            END IF;

            UPDATE book_copies SET status = 'on_loan' WHERE copy_id = NEW.copy_id;
        END IF;

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

-- 8. ПРЕДСТАВЛЕННЯ (VIEWS)
--DROP VIEW IF EXISTS v_loans_by_genre, v_monthly_loans, v_popular_books,
                    --v_overdue_loans, v_active_loans, v_book_catalog;

CREATE OR REPLACE VIEW v_book_catalog AS
SELECT
    b.book_id,
    b.title,
    b.isbn,
    b.publication_year,
    b.publisher,
    b.language,
    b.cover_url,
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
SELECT l.loan_id, l.loaned_at, l.due_date, l.renewal_count,
       u.user_id AS reader_id, u.full_name AS reader_name, u.email AS reader_email,
       c.copy_id, c.inventory_number, b.book_id, b.title,
       loc.room, loc.shelf,
       GREATEST(CURRENT_DATE - l.due_date, 0) AS days_overdue
FROM loans l
JOIN users u            ON u.user_id = l.reader_id
JOIN book_copies c      ON c.copy_id = l.copy_id
JOIN books b            ON b.book_id = c.book_id
LEFT JOIN locations loc ON loc.location_id = c.location_id
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

-- 9. ВИПРАВЛЕННЯ НАЯВНИХ ДАНИХ
-- Старий тригер лишав примірник on_loan навіть після повернення
-- (історичний запис). Повертаємо available там, де активної видачі немає.
UPDATE book_copies c
SET status = 'available'
WHERE c.status = 'on_loan'
  AND NOT EXISTS (SELECT 1 FROM loans l WHERE l.copy_id = c.copy_id AND l.returned_at IS NULL);

-- Стартові місця зберігання (лише якщо таблиця порожня)
INSERT INTO locations (room, shelf)
SELECT v.room, v.shelf
FROM (VALUES ('Головний зал', 'Стелаж 1A'),
             ('Головний зал', 'Стелаж 2B'),
             ('Сховище',      'Полиця 10')) AS v(room, shelf)
WHERE NOT EXISTS (SELECT 1 FROM locations);

-- Прив'язка тестових примірників без місця (ваші власні не зачіпаються)
UPDATE book_copies c
SET location_id = loc.location_id
FROM (VALUES ('INV-00101', 'Головний зал', 'Стелаж 1A'),
             ('INV-00102', 'Головний зал', 'Стелаж 1A'),
             ('INV-00201', 'Головний зал', 'Стелаж 2B'),
             ('INV-00301', 'Головний зал', 'Стелаж 2B'),
             ('INV-00302', 'Головний зал', 'Стелаж 2B'),
             ('INV-00401', 'Головний зал', 'Стелаж 1A'),
             ('INV-00501', 'Сховище',      'Полиця 10')) AS m(inv, room, shelf)
JOIN locations loc ON loc.room = m.room AND loc.shelf = m.shelf
WHERE c.inventory_number = m.inv
  AND c.location_id IS NULL;

-- 10. ТЕСТОВІ ДАНІ: 
DO $seed$
BEGIN
    IF EXISTS (SELECT 1 FROM users) OR EXISTS (SELECT 1 FROM books) THEN
        RAISE NOTICE 'Тестові дані пропущено: у базі вже є користувачі або книги';
    ELSE
        INSERT INTO users (full_name, email, password_hash, role) VALUES
        ('Олена Бондаренко',  'olena.b@gmail.com',         'pbkdf2-sha256$210000$eSmTTHQLnU28EhmlAy+2Ww==$PtZIag7Bzhfm8uKde6lPJbt2B9Y2/+sW+Ip31JSQWDE=', 'reader'),
        ('Іван Ковальчук',    'ivan.k@gmail.com',          'pbkdf2-sha256$210000$eSmTTHQLnU28EhmlAy+2Ww==$PtZIag7Bzhfm8uKde6lPJbt2B9Y2/+sW+Ip31JSQWDE=', 'reader'),
        ('Марія Ткаченко',    'maria.t@gmail.com',         'pbkdf2-sha256$210000$eSmTTHQLnU28EhmlAy+2Ww==$PtZIag7Bzhfm8uKde6lPJbt2B9Y2/+sW+Ip31JSQWDE=', 'reader'),
        ('Оксана Мандзювата', 'o.mandzyuvata@smartlib.ua', 'pbkdf2-sha256$210000$eSmTTHQLnU28EhmlAy+2Ww==$PtZIag7Bzhfm8uKde6lPJbt2B9Y2/+sW+Ip31JSQWDE=', 'librarian'),
        ('Софія Шелеп',       's.shelep@smartlib.ua',      'pbkdf2-sha256$210000$eSmTTHQLnU28EhmlAy+2Ww==$PtZIag7Bzhfm8uKde6lPJbt2B9Y2/+sW+Ip31JSQWDE=', 'admin');

        INSERT INTO authors (full_name, birth_year, country) VALUES
        ('Тарас Шевченко',  1814, 'Україна'),
        ('Іван Франко',     1856, 'Україна'),
        ('Джордж Орвелл',   1903, 'Велика Британія'),
        ('Дж. К. Роулінг',  1965, 'Велика Британія'),
        ('Франц Кафка',     1883, 'Австро-Угорщина')
        ON CONFLICT DO NOTHING;

        INSERT INTO genres (name) VALUES
        ('Класична література'), ('Поезія'), ('Антиутопія'), ('Фентезі'), ('Психологічна проза')
        ON CONFLICT DO NOTHING;

        INSERT INTO books (title, isbn, publication_year, publisher, description, language) VALUES
        ('Кобзар',                             '978-966-03-8001-1', 2020, 'Фоліо',                    'Збірка поетичних творів Тараса Шевченка.', 'uk'),
        ('Захар Беркут',                       '978-966-03-8002-8', 2019, 'Школа',                    'Історична повість про боротьбу общини проти монгольської навали.', 'uk'),
        ('1984',                               '978-617-7538-01-0', 2021, 'Видавництво Старого Лева', 'Роман-антиутопія про тоталітарне суспільство.', 'uk'),
        ('Гаррі Поттер і філософський камінь', '978-617-585-047-3', 2022, 'А-БА-БА-ГА-ЛА-МА-ГА',      'Перша книга про пригоди юного чарівника.', 'uk'),
        ('Перевтілення',                       '978-966-03-8003-5', 2018, 'Фоліо',                    'Новела про відчуження людини в сучасному світі.', 'uk');

        -- книга <-> автор
        INSERT INTO book_authors (book_id, author_id)
        SELECT b.book_id, a.author_id
        FROM (VALUES ('978-966-03-8001-1', 'Тарас Шевченко'),
                     ('978-966-03-8002-8', 'Іван Франко'),
                     ('978-617-7538-01-0', 'Джордж Орвелл'),
                     ('978-617-585-047-3', 'Дж. К. Роулінг'),
                     ('978-966-03-8003-5', 'Франц Кафка')) AS v(isbn, author)
        JOIN books b   ON b.isbn = v.isbn
        JOIN authors a ON a.full_name = v.author;

        -- книга <-> жанр
        INSERT INTO book_genres (book_id, genre_id)
        SELECT b.book_id, g.genre_id
        FROM (VALUES ('978-966-03-8001-1', 'Класична література'),
                     ('978-966-03-8001-1', 'Поезія'),
                     ('978-966-03-8002-8', 'Класична література'),
                     ('978-617-7538-01-0', 'Антиутопія'),
                     ('978-617-585-047-3', 'Фентезі'),
                     ('978-966-03-8003-5', 'Класична література'),
                     ('978-966-03-8003-5', 'Психологічна проза')) AS v(isbn, genre)
        JOIN books b  ON b.isbn = v.isbn
        JOIN genres g ON g.name = v.genre;

        -- примірники
        INSERT INTO book_copies (book_id, location_id, inventory_number, status, written_off_at, write_off_reason)
        SELECT b.book_id, loc.location_id, v.inv, v.st::copy_status,
               CASE WHEN v.st = 'written_off' THEN CURRENT_DATE END,
               v.reason
        FROM (VALUES ('978-966-03-8001-1', 'INV-00101', 'available',   'Головний зал', 'Стелаж 1A', NULL),
                     ('978-966-03-8001-1', 'INV-00102', 'available',   'Головний зал', 'Стелаж 1A', NULL),
                     ('978-966-03-8002-8', 'INV-00201', 'available',   'Головний зал', 'Стелаж 2B', NULL),
                     ('978-617-7538-01-0', 'INV-00301', 'available',   'Головний зал', 'Стелаж 2B', NULL),
                     ('978-617-7538-01-0', 'INV-00302', 'available',   'Головний зал', 'Стелаж 2B', NULL),
                     ('978-617-585-047-3', 'INV-00401', 'available',   'Головний зал', 'Стелаж 1A', NULL),
                     ('978-966-03-8003-5', 'INV-00501', 'written_off', 'Сховище',      'Полиця 10', 'Пошкодження сторінок'))
             AS v(isbn, inv, st, room, shelf, reason)
        JOIN books b            ON b.isbn = v.isbn
        LEFT JOIN locations loc ON loc.room = v.room AND loc.shelf = v.shelf;

        -- Видача 1: повернена (історичний запис), примірник лишається доступним
        INSERT INTO loans (copy_id, reader_id, librarian_id, loaned_at, due_date, returned_at)
        SELECT (SELECT copy_id FROM book_copies WHERE inventory_number = 'INV-00101'),
               (SELECT user_id FROM users WHERE email = 'olena.b@gmail.com'),
               (SELECT user_id FROM users WHERE email = 'o.mandzyuvata@smartlib.ua'),
               NOW() - INTERVAL '30 days', CURRENT_DATE - 16, NOW() - INTERVAL '15 days';

        -- Видача 2: активна, у межах терміну
        INSERT INTO loans (copy_id, reader_id, librarian_id, loaned_at, due_date)
        SELECT (SELECT copy_id FROM book_copies WHERE inventory_number = 'INV-00201'),
               (SELECT user_id FROM users WHERE email = 'olena.b@gmail.com'),
               (SELECT user_id FROM users WHERE email = 'o.mandzyuvata@smartlib.ua'),
               NOW() - INTERVAL '5 days', CURRENT_DATE + 9;

        -- Видача 3: активна, прострочена
        INSERT INTO loans (copy_id, reader_id, librarian_id, loaned_at, due_date)
        SELECT (SELECT copy_id FROM book_copies WHERE inventory_number = 'INV-00301'),
               (SELECT user_id FROM users WHERE email = 'ivan.k@gmail.com'),
               (SELECT user_id FROM users WHERE email = 'o.mandzyuvata@smartlib.ua'),
               NOW() - INTERVAL '20 days', CURRENT_DATE - 6;

        -- Штраф за прострочену видачу 3
        INSERT INTO fines (loan_id, amount, is_paid)
        SELECT l.loan_id, 50.00, FALSE
        FROM loans l
        JOIN book_copies c ON c.copy_id = l.copy_id
        WHERE c.inventory_number = 'INV-00301' AND l.returned_at IS NULL;

        INSERT INTO reservations (book_id, reader_id, reserved_at, expires_at, status)
        SELECT (SELECT book_id FROM books WHERE isbn = '978-966-03-8002-8'),
               (SELECT user_id FROM users WHERE email = 'maria.t@gmail.com'),
               NOW(), NOW() + INTERVAL '3 days', 'active';
    END IF;
END
$seed$;

COMMIT;
