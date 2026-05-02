// ─── Tab navigation ──────────────────────────────────────────────────────────

document.querySelectorAll(".tab").forEach((tab) => {
    tab.addEventListener("click", () => {
        document.querySelectorAll(".tab").forEach((t) => {
            t.classList.remove("active");
            t.setAttribute("aria-selected", "false");
        });
        document.querySelectorAll(".tab-panel").forEach((p) => p.classList.add("hidden"));

        tab.classList.add("active");
        tab.setAttribute("aria-selected", "true");

        const panelId = `tab-${tab.dataset.tab}`;
        document.getElementById(panelId).classList.remove("hidden");

        // Load communities list when switching to Browse or Compare for the first time
        if (tab.dataset.tab === "browse") loadBrowseList();
        if (tab.dataset.tab === "compare") loadCompareList();
    });
});

// ─── Shared helpers ───────────────────────────────────────────────────────────

async function readResponse(response) {
    const ct = response.headers.get("content-type") || "";
    return ct.includes("application/json") ? response.json() : response.text();
}

function extractErrorMessage(data) {
    if (typeof data === "string" && data.trim()) return data;
    if (data?.detail) return data.detail;
    if (data?.title)  return data.title;
    return "Сталася невідома помилка.";
}

function showStatus(elementId, message, type) {
    const box = document.getElementById(elementId);
    box.textContent = message;
    box.className = "status" + (type ? ` ${type}` : "");
}

function escapeHtml(value) {
    return String(value)
        .replaceAll("&", "&amp;")
        .replaceAll("<", "&lt;")
        .replaceAll(">", "&gt;")
        .replaceAll('"', "&quot;")
        .replaceAll("'", "&#39;");
}

// Renders summary tiles + sections — used by Upload result, Browse detail, and Compare columns
function renderDocumentContent(summaryEl, sectionsEl, data) {
    const summaryItems = [
        { label: "Громада",       value: data.communityName || "-" },
        { label: "Регіон",        value: data.region        || "-" },
        { label: "Рік",           value: data.year          || "-" },
        { label: "Тип документа", value: data.docType       || "-" },
        { label: "Секцій",        value: data.metadata?.totalSections ?? data.sections?.length ?? 0 },
        { label: "Час обробки",   value: `${data.metadata?.processingTimeMs ?? data.processingTimeMs ?? 0} ms` },
    ];

    summaryEl.innerHTML = summaryItems
        .map((i) => `<div class="summary-item">
            <strong>${escapeHtml(i.label)}</strong>
            <span>${escapeHtml(String(i.value))}</span>
        </div>`)
        .join("");

    const sections = Array.isArray(data.sections) ? data.sections : [];
    sectionsEl.innerHTML = sections.length
        ? sections.map((s, idx) => `
            <article class="section-card">
                <h3>${escapeHtml(s.title || `Секція ${idx + 1}`)}</h3>
                <div class="section-meta">Рівень заголовка: ${escapeHtml(String(s.level ?? 0))}</div>
                <p>${escapeHtml(s.content || "Без тексту")}</p>
            </article>`).join("")
        : "<p>Секції не знайдено.</p>";
}

// Builds an expandable community → documents tree for Browse and Compare tabs
function renderCommunitiesList(containerId, communities, onDocumentClick) {
    const container = document.getElementById(containerId);

    if (!communities.length) {
        container.innerHTML = "<p class='empty-hint'>Жодного документу ще не збережено.</p>";
        return;
    }

    container.innerHTML = communities.map((c) => `
        <div class="community-row">
            <button class="community-toggle" data-id="${escapeHtml(c.id)}">
                <span class="community-name">${escapeHtml(c.name)}</span>
                <span class="community-region">${escapeHtml(c.region)}</span>
                <span class="chevron">▾</span>
            </button>
            <ul class="doc-list hidden" id="docs-${escapeHtml(c.id)}">
                ${c.documents.map((d) => `
                    <li>
                        <button class="doc-item" data-doc-id="${escapeHtml(d.id)}">
                            <span class="doc-year">${escapeHtml(String(d.year))}</span>
                            <span class="doc-type">${escapeHtml(d.docType)}</span>
                            <span class="doc-file">${escapeHtml(d.fileName)}</span>
                            <span class="doc-status badge">${escapeHtml(d.status)}</span>
                        </button>
                    </li>`).join("")}
            </ul>
        </div>`).join("");

    // Toggle community expand/collapse
    container.querySelectorAll(".community-toggle").forEach((btn) => {
        btn.addEventListener("click", () => {
            const list = document.getElementById(`docs-${btn.dataset.id}`);
            const isOpen = !list.classList.contains("hidden");
            list.classList.toggle("hidden", isOpen);
            btn.querySelector(".chevron").textContent = isOpen ? "▾" : "▴";
        });
    });

    // Wire document click
    container.querySelectorAll(".doc-item").forEach((btn) => {
        btn.addEventListener("click", () => onDocumentClick(btn.dataset.docId));
    });
}

// ─── TAB: UPLOAD ─────────────────────────────────────────────────────────────

const uploadForm   = document.getElementById("upload-form");
const submitButton = uploadForm.querySelector("button[type='submit']");

uploadForm.addEventListener("submit", async (event) => {
    event.preventDefault();

    const formData = new FormData(uploadForm);
    const file = formData.get("file");

    if (!(file instanceof File) || file.size === 0) {
        showStatus("upload-status", "Оберіть файл перед відправкою.", "error");
        document.getElementById("upload-result-card").classList.add("hidden");
        return;
    }

    submitButton.disabled = true;
    submitButton.textContent = "Обробляємо...";
    showStatus("upload-status", "Документ завантажується та обробляється...", "");
    document.getElementById("upload-result-card").classList.add("hidden");

    try {
        const response = await fetch("/api/upload", { method: "POST", body: formData });
        const data = await readResponse(response);

        if (!response.ok) throw new Error(extractErrorMessage(data));

        const card   = document.getElementById("upload-result-card");
        const badge  = document.getElementById("upload-result-badge");
        card.classList.remove("hidden");
        badge.textContent = data.metadata?.status || "parsed";

        renderDocumentContent(
            document.getElementById("upload-result-summary"),
            document.getElementById("upload-sections"),
            data
        );

        showStatus("upload-status", "Документ успішно оброблено.", "success");
        uploadForm.reset();
    } catch (error) {
        showStatus("upload-status", error.message || "Сталася невідома помилка.", "error");
    } finally {
        submitButton.disabled = false;
        submitButton.textContent = "Надіслати на обробку";
    }
});

// ─── TAB: BROWSE ─────────────────────────────────────────────────────────────

let browseLoaded = false;

async function loadBrowseList() {
    if (browseLoaded) return;
    browseLoaded = true;
    showStatus("browse-status", "Завантаження списку...", "");

    try {
        const res  = await fetch("/api/communities");
        const data = await readResponse(res);
        if (!res.ok) throw new Error(extractErrorMessage(data));

        showStatus("browse-status", "", "");
        renderCommunitiesList("communities-list", data, openBrowseDetail);
    } catch (err) {
        showStatus("browse-status", err.message || "Не вдалося завантажити список.", "error");
        browseLoaded = false;
    }
}

// Force-reload on refresh button
document.getElementById("browse-refresh").addEventListener("click", () => {
    browseLoaded = false;
    document.getElementById("communities-list").innerHTML = "";
    document.getElementById("browse-detail-card").classList.add("hidden");
    loadBrowseList();
});

async function openBrowseDetail(documentId) {
    showStatus("browse-status", "Завантаження документу...", "");
    document.getElementById("browse-detail-card").classList.add("hidden");

    try {
        const res  = await fetch(`/api/communities/documents/${documentId}`);
        const data = await readResponse(res);
        if (!res.ok) throw new Error(extractErrorMessage(data));

        showStatus("browse-status", "", "");

        const card  = document.getElementById("browse-detail-card");
        const title = document.getElementById("browse-detail-title");
        const badge = document.getElementById("browse-detail-badge");

        title.textContent = `${data.communityName} — ${data.year}`;
        badge.textContent = data.status || "parsed";
        card.classList.remove("hidden");

        renderDocumentContent(
            document.getElementById("browse-detail-summary"),
            document.getElementById("browse-detail-sections"),
            data
        );

        card.scrollIntoView({ behavior: "smooth", block: "start" });
    } catch (err) {
        showStatus("browse-status", err.message || "Помилка завантаження документу.", "error");
    }
}

// ─── TAB: COMPARE ────────────────────────────────────────────────────────────

let compareLoaded  = false;
const compareSlots = { a: null, b: null }; // { id, communityName, year, docType }

async function loadCompareList() {
    if (compareLoaded) return;
    compareLoaded = true;
    showStatus("compare-status", "Завантаження списку...", "");

    try {
        const res  = await fetch("/api/communities");
        const data = await readResponse(res);
        if (!res.ok) throw new Error(extractErrorMessage(data));

        showStatus("compare-status", "", "");
        renderCommunitiesList("compare-communities-list", data, selectForCompare);
    } catch (err) {
        showStatus("compare-status", err.message || "Не вдалося завантажити список.", "error");
        compareLoaded = false;
    }
}

function selectForCompare(documentId) {
    // Find document metadata from the rendered list
    const btn = document.querySelector(`#compare-communities-list [data-doc-id="${documentId}"]`);
    if (!btn) return;

    const communityName = btn.closest(".community-row")
        ?.querySelector(".community-name")?.textContent || "";
    const year    = btn.querySelector(".doc-year")?.textContent || "";
    const docType = btn.querySelector(".doc-type")?.textContent || "";

    const label = `${communityName} · ${year} · ${docType}`;

    // Fill slot A first, then B
    if (!compareSlots.a) {
        compareSlots.a = { id: documentId, label };
        updateSlotUI("a");
    } else if (!compareSlots.b && documentId !== compareSlots.a.id) {
        compareSlots.b = { id: documentId, label };
        updateSlotUI("b");
        runCompare();
    } else if (documentId === compareSlots.a?.id || documentId === compareSlots.b?.id) {
        showStatus("compare-status", "Цей документ вже обрано.", "error");
    } else {
        showStatus("compare-status", "Обидва слоти заповнені. Очистіть один перед новим вибором.", "error");
    }
}

function updateSlotUI(slot) {
    const info        = document.getElementById(`slot-${slot}-info`);
    const placeholder = document.getElementById(`slot-${slot}-placeholder`);
    const clearBtn    = document.getElementById(`clear-${slot}`);
    const data        = compareSlots[slot];

    if (data) {
        info.textContent = data.label;
        info.classList.remove("hidden");
        placeholder.classList.add("hidden");
        clearBtn.classList.remove("hidden");
    } else {
        info.textContent = "";
        info.classList.add("hidden");
        placeholder.classList.remove("hidden");
        clearBtn.classList.add("hidden");
    }
}

["a", "b"].forEach((slot) => {
    document.getElementById(`clear-${slot}`).addEventListener("click", () => {
        compareSlots[slot] = null;
        updateSlotUI(slot);
        document.getElementById("compare-result").classList.add("hidden");
        showStatus("compare-status", "", "");
    });
});

async function runCompare() {
    showStatus("compare-status", "Завантаження документів для порівняння...", "");
    document.getElementById("compare-result").classList.add("hidden");

    try {
        const [resA, resB] = await Promise.all([
            fetch(`/api/communities/documents/${compareSlots.a.id}`),
            fetch(`/api/communities/documents/${compareSlots.b.id}`),
        ]);

        const [docA, docB] = await Promise.all([readResponse(resA), readResponse(resB)]);

        if (!resA.ok) throw new Error(extractErrorMessage(docA));
        if (!resB.ok) throw new Error(extractErrorMessage(docB));

        renderCompare(docA, docB);
        showStatus("compare-status", "", "");
    } catch (err) {
        showStatus("compare-status", err.message || "Помилка завантаження.", "error");
    }
}

function renderCompare(docA, docB) {
    const result  = document.getElementById("compare-result");
    const columns = document.getElementById("compare-columns");

    columns.innerHTML = [docA, docB].map((doc) => `
        <div class="compare-col">
            <div class="compare-col-header">
                <strong>${escapeHtml(doc.communityName)}</strong>
                <span>${escapeHtml(String(doc.year))} · ${escapeHtml(doc.docType)}</span>
                <span class="badge">${escapeHtml(doc.status)}</span>
            </div>
            <div class="summary compare-summary">
                ${[
        { label: "Регіон",   value: doc.region },
        { label: "Формат",   value: doc.fileFormat },
        { label: "Секцій",   value: doc.sections?.length ?? 0 },
        { label: "Обробка",  value: `${doc.processingTimeMs ?? 0} ms` },
    ].map((i) => `<div class="summary-item">
                    <strong>${escapeHtml(i.label)}</strong>
                    <span>${escapeHtml(String(i.value))}</span>
                </div>`).join("")}
            </div>
            <div class="sections">
                ${(doc.sections || []).map((s, idx) => `
                    <article class="section-card">
                        <h3>${escapeHtml(s.title || `Секція ${idx + 1}`)}</h3>
                        <div class="section-meta">Рівень: ${escapeHtml(String(s.level ?? 0))}</div>
                        <p>${escapeHtml(s.content || "Без тексту")}</p>
                    </article>`).join("") || "<p>Секції не знайдено.</p>"}
            </div>
        </div>`).join("");

    result.classList.remove("hidden");
    result.scrollIntoView({ behavior: "smooth", block: "start" });
}