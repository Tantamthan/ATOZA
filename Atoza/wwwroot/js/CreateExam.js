let finalExamData = [];

const textArea = document.getElementById("rawContent");
const previewDiv = document.getElementById("previewContent");
const questionCountEl = document.getElementById("questionCount");
const answeredCountEl = document.getElementById("answeredCount");
const issueCountEl = document.getElementById("issueCount");
const parseStatusEl = document.getElementById("parseStatus");

async function simulateUpload() {
    const filePath = "/data/TextFile1.txt";

    try {
        const response = await fetch(filePath);
        const sampleText = await response.text();
        textArea.value = sampleText.trim();
        processText();
    } catch (error) {
        updateParseStatus("Khong the tai file mau luc nay.", "warning");
    }
}

if (textArea) {
    let timeout = null;
    textArea.addEventListener("input", () => {
        clearTimeout(timeout);
        timeout = setTimeout(processText, 220);
    });
}

function processText() {
    if (!textArea || !previewDiv) {
        return;
    }

    const lines = textArea.value
        .replace(/\r\n/g, "\n")
        .replace(/\r/g, "\n")
        .split("\n");

    finalExamData = [];
    let currentQuestion = null;
    let questionIndex = 0;

    const flushQuestion = () => {
        if (!currentQuestion) {
            return;
        }

        finalizeQuestion(currentQuestion);
        currentQuestion.id = `q-${questionIndex}`;
        finalExamData.push(currentQuestion);
        currentQuestion = null;
    };

    lines.forEach((rawLine, lineIndex) => {
        const line = normalizeLine(rawLine);
        if (!line) {
            return;
        }

        const questionMatch = line.match(/^(?:cau|câu|question|q)\s*\d+\s*[:.)-]?\s*(.+)$/i) ||
            line.match(/^(\d+)\s*[.)]\s*(.+)$/);

        if (questionMatch) {
            flushQuestion();
            questionIndex += 1;

            const questionContent = (questionMatch[1] && questionMatch[2]) ? questionMatch[2] : questionMatch[1];
            currentQuestion = {
                questionNumber: questionIndex,
                questionText: questionContent.trim(),
                options: [],
                correctKey: null,
                rawStart: getRawStart(lines, lineIndex)
            };
            return;
        }

        if (!currentQuestion) {
            return;
        }

        const optionMatch = line.match(/^(\*)?\s*([A-Da-d])\s*[.):-]\s*(.+)$/);
        if (optionMatch) {
            const key = optionMatch[2].toUpperCase();
            const content = optionMatch[3].trim();
            const isCorrect = Boolean(optionMatch[1]);
            const existing = currentQuestion.options.find((option) => option.key === key);

            if (existing) {
                existing.content = content;
                existing.isCorrect = isCorrect;
            } else {
                currentQuestion.options.push({ key, content, isCorrect });
            }

            if (isCorrect) {
                currentQuestion.correctKey = key;
            }
            return;
        }

        const answerMatch = line.match(/^(?:dap an|đáp án|answer)\s*[:\-]?\s*([A-Da-d])$/i);
        if (answerMatch) {
            currentQuestion.correctKey = answerMatch[1].toUpperCase();
            return;
        }

        if (currentQuestion.options.length > 0) {
            const lastOption = currentQuestion.options[currentQuestion.options.length - 1];
            lastOption.content = `${lastOption.content} ${line}`.trim();
            return;
        }

        currentQuestion.questionText = `${currentQuestion.questionText} ${line}`.trim();
    });

    flushQuestion();
    syncCorrectState();
    renderPreview();
    updateSummary();
}

function finalizeQuestion(question) {
    question.questionText = sanitizeInlineText(question.questionText);
    question.options = question.options
        .map((option) => ({
            key: option.key,
            content: sanitizeInlineText(option.content),
            isCorrect: Boolean(option.isCorrect)
        }))
        .sort((left, right) => left.key.localeCompare(right.key));
}

function syncCorrectState() {
    finalExamData.forEach((question) => {
        question.options.forEach((option) => {
            option.isCorrect = option.key === question.correctKey;
        });
    });
}

function renderPreview() {
    if (!previewDiv) {
        return;
    }

    if (finalExamData.length === 0) {
        previewDiv.innerHTML = `
            <div class="empty-state">
                <h3>Chua co cau hoi nao duoc nhan dien</h3>
                <p>Dinh dang de theo mau "Cau 1", "A.", "B.", "C.", "D." de he thong xu ly on dinh hon.</p>
            </div>`;
        return;
    }

    const html = finalExamData.map((question, index) => {
        const issueList = getQuestionIssues(question);
        const optionsHtml = question.options.map((option) => `
            <button type="button"
                class="option-chip ${option.isCorrect ? "is-correct" : ""}"
                data-q-index="${index}"
                data-opt-key="${option.key}">
                <span>${option.key}</span>
                <strong>${escapeHtml(option.content)}</strong>
            </button>`).join("");

        const issueHtml = issueList.length
            ? `<div class="question-issues">${issueList.map((issue) => `<span>${escapeHtml(issue)}</span>`).join("")}</div>`
            : `<div class="question-issues ok"><span>San sang luu</span></div>`;

        return `
            <article class="question-card" data-raw-start="${question.rawStart}">
                <div class="question-card__top">
                    <div>
                        <p class="question-label">Cau ${question.questionNumber}</p>
                        <h3>${escapeHtml(question.questionText)}</h3>
                    </div>
                    <button type="button" class="jump-link" onclick="scrollToRaw(${question.rawStart})">Den noi dung</button>
                </div>
                <div class="option-grid">${optionsHtml}</div>
                ${issueHtml}
            </article>`;
    }).join("");

    previewDiv.innerHTML = html;

    document.querySelectorAll(".option-chip").forEach((item) => {
        item.addEventListener("click", toggleAnswer);
    });
}

function updateSummary() {
    const answeredCount = finalExamData.filter((question) => question.correctKey).length;
    const issueCount = finalExamData.reduce((total, question) => total + getQuestionIssues(question).length, 0);

    if (questionCountEl) {
        questionCountEl.textContent = String(finalExamData.length);
    }

    if (answeredCountEl) {
        answeredCountEl.textContent = String(answeredCount);
    }

    if (issueCountEl) {
        issueCountEl.textContent = String(issueCount);
    }

    if (finalExamData.length === 0) {
        updateParseStatus("Dang cho noi dung de thi...", "idle");
        return;
    }

    if (issueCount === 0) {
        updateParseStatus("Parser da nhan dien tot. Ban co the kiem tra nhanh roi luu de.", "success");
        return;
    }

    updateParseStatus("Da parse xong nhung van con mot vai cau can kiem tra truoc khi luu.", "warning");
}

function updateParseStatus(message, state) {
    if (!parseStatusEl) {
        return;
    }

    parseStatusEl.textContent = message;
    parseStatusEl.dataset.state = state;
}

function getQuestionIssues(question) {
    const issues = [];

    if (!question.questionText) {
        issues.push("Thieu noi dung cau hoi");
    }

    if (question.options.length < 4) {
        issues.push("Chua du 4 dap an A-D");
    }

    if (!question.correctKey) {
        issues.push("Chua chon dap an dung");
    }

    return issues;
}

function toggleAnswer(event) {
    const optionElement = event.currentTarget;
    const qIndex = Number(optionElement.getAttribute("data-q-index"));
    const key = optionElement.getAttribute("data-opt-key");

    const currentQuestion = finalExamData[qIndex];
    if (!currentQuestion || !key) {
        return;
    }

    currentQuestion.correctKey = currentQuestion.correctKey === key ? null : key;
    syncCorrectState();
    rewriteTextareaFromData();
    renderPreview();
    updateSummary();
}

function rewriteTextareaFromData() {
    if (!textArea) {
        return;
    }

    const normalized = finalExamData.map((question, index) => {
        const lines = [`Cau ${index + 1}: ${question.questionText}`];
        ["A", "B", "C", "D"].forEach((key) => {
            const option = question.options.find((item) => item.key === key);
            if (option) {
                const marker = question.correctKey === key ? "*" : "";
                lines.push(`${marker}${key}. ${option.content}`);
            }
        });
        return lines.join("\n");
    });

    textArea.value = normalized.join("\n\n");
}

function scrollToRaw(rawStart) {
    if (!textArea) {
        return;
    }

    textArea.focus();
    textArea.setSelectionRange(rawStart, rawStart);

    const approximateLineHeight = 24;
    const prefix = textArea.value.slice(0, rawStart);
    const lineCount = prefix.split("\n").length - 1;
    textArea.scrollTop = Math.max(0, (lineCount * approximateLineHeight) - (textArea.clientHeight / 2));
}

function saveExam() {
    if (finalExamData.length === 0) {
        alert("Chua co du lieu de thi.");
        return;
    }

    showExamModal();
}

function clearErrors() {
    document.querySelectorAll(".error-message").forEach((element) => {
        element.style.display = "none";
        element.innerText = "";
    });
}

function showExamModal() {
    if (finalExamData.length === 0) {
        alert("Chua co cau hoi nao duoc tao.");
        return;
    }

    const totalEl = document.getElementById("totalQuestionsCount");
    if (totalEl) {
        totalEl.innerText = String(finalExamData.length);
    }

    document.getElementById("examModal").style.display = "flex";
}

function closeModal() {
    document.getElementById("examModal").style.display = "none";
    document.getElementById("examTitle").value = "";
    document.getElementById("examDuration").value = "";
    document.querySelector('input[name="examType"][value="exam"]').checked = true;
    const publicCheckbox = document.getElementById("examIsPublic");
    if (publicCheckbox) {
        publicCheckbox.checked = false;
    }
    clearErrors();
}

function normalizeLine(line) {
    return line
        .replace(/\u00a0/g, " ")
        .replace(/\t/g, " ")
        .replace(/\s+/g, " ")
        .trim();
}

function sanitizeInlineText(text) {
    return text.replace(/\s+/g, " ").trim();
}

function getRawStart(lines, lineIndex) {
    let total = 0;
    for (let i = 0; i < lineIndex; i += 1) {
        total += lines[i].length + 1;
    }
    return total;
}

function escapeHtml(value) {
    return value
        .replace(/&/g, "&amp;")
        .replace(/</g, "&lt;")
        .replace(/>/g, "&gt;")
        .replace(/"/g, "&quot;")
        .replace(/'/g, "&#39;");
}

window.showExamModal = showExamModal;
window.closeModal = closeModal;
window.saveExam = saveExam;
window.simulateUpload = simulateUpload;
window.scrollToRaw = scrollToRaw;
window.processText = processText;
