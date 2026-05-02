-- ============================================================
-- LocalGovProcessor — Database Schema
-- PostgreSQL 15+
-- ============================================================

-- Громади — базова одиниця. Одна громада може мати багато документів.
CREATE TABLE communities (
    id          UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    name        VARCHAR(255) NOT NULL,
    region      VARCHAR(255) NOT NULL,
    created_at  TIMESTAMPTZ  NOT NULL DEFAULT now(),

    CONSTRAINT uq_community UNIQUE (name, region)
);

-- Завантажені документи. Зберігає сирий текст та статус обробки.
-- Розширення: колонки llm_processed_at, goals_json додаються пізніше,
-- коли LLM-пайплайн буде готовий — без зміни існуючих колонок.
CREATE TABLE documents (
    id                  UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    community_id        UUID         NOT NULL REFERENCES communities(id) ON DELETE CASCADE,

    year                SMALLINT     NOT NULL CHECK (year BETWEEN 2000 AND 2100),
    doc_type            VARCHAR(64)  NOT NULL, -- strategic_plan | budget | council_decision
    file_name           VARCHAR(255) NOT NULL,
    file_format         VARCHAR(8)   NOT NULL, -- docx | pdf

    status              VARCHAR(32)  NOT NULL DEFAULT 'parsed',
    -- parsed | processing | processed | processing_failed | low_quality

    raw_text            TEXT,        -- сирий текст після парсингу (до LLM)
    processing_time_ms  INTEGER,

    uploaded_at         TIMESTAMPTZ  NOT NULL DEFAULT now(),

    -- Запобігає дублікатам одного документу
    CONSTRAINT uq_document UNIQUE (community_id, year, doc_type, file_name)
);

-- Секції документу — результат парсингу (DOCX з рівнями / PDF посторінково).
-- Зберігається окремо від raw_text для зручної вибірки та майбутнього порівняння.
CREATE TABLE sections (
    id          UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    document_id UUID         NOT NULL REFERENCES documents(id) ON DELETE CASCADE,

    position    SMALLINT     NOT NULL, -- порядковий номер секції в документі
    level       SMALLINT     NOT NULL, -- 0 = без заголовка / сторінка PDF, 1–3 = рівень заголовка
    title       TEXT,
    content     TEXT,

    CONSTRAINT uq_section UNIQUE (document_id, position)
);

-- ============================================================
-- Індекси для типових запитів
-- ============================================================

-- Фільтрація документів по громаді та році
CREATE INDEX idx_documents_community ON documents(community_id);
CREATE INDEX idx_documents_year      ON documents(year);
CREATE INDEX idx_documents_status    ON documents(status);

-- Вибірка секцій конкретного документу в правильному порядку
CREATE INDEX idx_sections_document   ON sections(document_id, position);

-- Пошук громад по регіону
CREATE INDEX idx_communities_region  ON communities(region);
