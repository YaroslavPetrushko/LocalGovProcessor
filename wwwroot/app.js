const form = document.getElementById("upload-form");
const statusBox = document.getElementById("status");
const resultCard = document.getElementById("result-card");
const resultBadge = document.getElementById("result-badge");
const resultSummary = document.getElementById("result-summary");
const sectionsContainer = document.getElementById("sections");
const submitButton = form.querySelector("button[type='submit']");

form.addEventListener("submit", async (event) => {
    event.preventDefault();

    const formData = new FormData(form);
    const file = formData.get("file");

    if (!(file instanceof File) || file.size === 0) {
        showStatus("Оберіть файл перед відправкою.", "error");
        hideResult();
        return;
    }

    submitButton.disabled = true;
    submitButton.textContent = "Обробляємо...";
    showStatus("Документ завантажується та обробляється...", "");
    hideResult();

    try {
        const response = await fetch("/api/upload", {
            method: "POST",
            body: formData
        });

        const data = await readResponse(response);

        if (!response.ok) {
            const message = extractErrorMessage(data);
            throw new Error(message);
        }

        renderResult(data);
        showStatus("Документ успішно оброблено.", "success");
        form.reset();
    } catch (error) {
        const message = error instanceof Error ? error.message : "Сталася невідома помилка.";
        showStatus(message, "error");
    } finally {
        submitButton.disabled = false;
        submitButton.textContent = "Надіслати на обробку";
    }
});

async function readResponse(response) {
    const contentType = response.headers.get("content-type") || "";

    if (contentType.includes("application/json")) {
        return response.json();
    }

    return response.text();
}

function extractErrorMessage(data) {
    if (typeof data === "string" && data.trim()) {
        return data;
    }

    if (data && typeof data === "object") {
        if (typeof data.detail === "string" && data.detail) {
            return data.detail;
        }

        if (typeof data.title === "string" && data.title) {
            return data.title;
        }
    }

    return "Не вдалося обробити документ.";
}

function renderResult(data) {
    resultCard.classList.remove("hidden");
    resultBadge.textContent = data.metadata?.status || "parsed";

    const summaryItems = [
        { label: "Громада", value: data.communityName || "-" },
        { label: "Регіон", value: data.region || "-" },
        { label: "Рік", value: data.year || "-" },
        { label: "Тип документа", value: data.docType || "-" },
        { label: "Секцій знайдено", value: data.metadata?.totalSections ?? 0 },
        { label: "Час обробки", value: `${data.metadata?.processingTimeMs ?? 0} ms` }
    ];

    resultSummary.innerHTML = summaryItems
        .map((item) => `
            <div class="summary-item">
                <strong>${escapeHtml(item.label)}</strong>
                <span>${escapeHtml(String(item.value))}</span>
            </div>
        `)
        .join("");

    const sections = Array.isArray(data.sections) ? data.sections : [];
    sectionsContainer.innerHTML = sections.length
        ? sections
            .map((section, index) => `
                <article class="section-card">
                    <h3>${escapeHtml(section.title || `Секція ${index + 1}`)}</h3>
                    <div class="section-meta">Рівень заголовка: ${escapeHtml(String(section.level ?? 0))}</div>
                    <p>${escapeHtml(section.content || "Без тексту")}</p>
                </article>
            `)
            .join("")
        : "<p>Секції не знайдено.</p>";
}

function showStatus(message, type) {
    statusBox.textContent = message;
    statusBox.className = "status";

    if (type) {
        statusBox.classList.add(type);
    }
}

function hideResult() {
    resultCard.classList.add("hidden");
    resultSummary.innerHTML = "";
    sectionsContainer.innerHTML = "";
}

function escapeHtml(value) {
    return value
        .replaceAll("&", "&amp;")
        .replaceAll("<", "&lt;")
        .replaceAll(">", "&gt;")
        .replaceAll("\"", "&quot;")
        .replaceAll("'", "&#39;");
}
