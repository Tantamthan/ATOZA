// 1. Biến lưu trữ dữ liệu JSON cuối cùng
let finalExamData = [];

// 2. Hàm giả lập Upload (Giả lập nội dung file mẫu)
async function simulateUpload() {
    const filePath = '/data/TextFile1.txt'
    let sampleText = "";
    try {
        const response = await fetch(filePath);
        sampleText = await response.text();
    } catch (e) {

    }

    document.getElementById('rawContent').value = sampleText.trim();
    processText(); // Kích hoạt xử lý ngay
}

// 3. Sự kiện lắng nghe khi người dùng gõ phím (Real-time Parsing)
const textArea = document.getElementById('rawContent');
const previewDiv = document.getElementById('previewContent');

// Sử dụng Debounce để không xử lý quá nhiều lần khi đang gõ nhanh
let timeout = null;
textArea.addEventListener('keyup', function () {
    clearTimeout(timeout);
    timeout = setTimeout(processText, 300); // Chờ 300ms sau khi ngừng gõ mới xử lý
});

// 4. CORE LOGIC: Xử lý văn bản thô thành JSON và Render HTML
function processText() {
    console.time("UltraFastParse");

    const text = textArea.value;
    finalExamData = []; // Reset dữ liệu gốc

    let htmlBuffer = "";
    let currentQ = null;
    let questionIndex = 0; // Thêm biến đếm câu hỏi

    // Helper: Hàm lưu câu hỏi cũ vào mảng và tạo HTML
    const flushQuestion = () => {
        if (!currentQ) return;

        const qId = `q-${questionIndex}`;
        currentQ.id = qId; // Gán ID cho câu hỏi (Để phục vụ cho tính năng tương tác)
        finalExamData.push(currentQ);

        // 2. Tạo HTML
        let optionsHtml = "";
        const len = currentQ.options.length;
        for (let i = 0; i < len; i++) {
            const opt = currentQ.options[i];
            const cls = opt.isCorrect ? 'option-item is-correct' : 'option-item';
            // Thêm data-q-index, data-opt-key để xử lý sự kiện click
            optionsHtml += `<li class="${cls}" data-q-index="${questionIndex - 1}" data-opt-key="${opt.key}">
                                <span class="option-key">${opt.key}.</span> ${opt.content}
                            </li>`;
        }

        // Thêm data-q-index và sự kiện onclick cho khối câu hỏi (Tính năng 1)
        //<span class="q-number">Câu ${questionIndex}:</span> 
        htmlBuffer += `<div class="question-block" id="${qId}" data-raw-start="${currentQ.rawStart}" onclick="scrollToRaw(${currentQ.rawStart})">
                            <div class="q-text">
                            ${currentQ.questionText}</div>
                            <ul class="options-list">${optionsHtml}</ul>
                            </div>`;
    };

    // --- BẮT ĐẦU QUÉT (SCANNING) ---
    let start = 0;
    let end = text.indexOf('\n');
    let line = "";
    let lineStart = 0; // Vị trí bắt đầu của dòng hiện tại trong nội dung thô

    while (end !== -1) {
        line = text.substring(start, end).trim();
        lineStart = start; // Ghi nhận vị trí bắt đầu của dòng

        if (line) {
            parseLine(line, lineStart);
        }

        start = end + 1;
        end = text.indexOf('\n', start);
    }
    // Xử lý dòng cuối cùng
    line = text.substring(start).trim();
    lineStart = start;
    if (line) parseLine(line, lineStart);

    // Lưu câu hỏi cuối cùng
    flushQuestion();

    // --- KẾT THÚC QUÉT ---

    // Hàm xử lý logic từng dòng
    function parseLine(str, rawStart) {
        const firstChar = str[0];

        // TRƯỜNG HỢP: Là câu hỏi "Câu ..." hoặc "Cau ..."
        if ((firstChar === 'C' || firstChar === 'c') && (str.startsWith("Câu ") || str.startsWith("Cau "))) {
            flushQuestion(); // Gặp câu mới -> Lưu câu cũ lại
            questionIndex++; // Tăng chỉ số câu hỏi

            // Tạo đối tượng câu mới
            currentQ = {
                questionText: str,
                options: [],
                correctKey: null,
                rawStart: rawStart // Vị trí bắt đầu của câu hỏi trong nội dung thô
            };
            return;
        }

        // Nếu chưa có câu hỏi nào được khởi tạo thì bỏ qua
        if (!currentQ) return;

        // TRƯỜNG HỢP: Là đáp án đúng (*A., *B., ...)
        if (firstChar === '*') {
            const key = str[1];
            const content = str.substring(3).trim();
            currentQ.correctKey = key;
            // Tìm và cập nhật trạng thái isCorrect cho đáp án cũ (nếu có)
            const existingOptIndex = currentQ.options.findIndex(opt => opt.key === key);
            if (existingOptIndex !== -1) {
                // Nếu đáp án *A. đã tồn tại là A., cập nhật lại
                currentQ.options[existingOptIndex] = { key: key, content: content, isCorrect: true };
            } else {
                currentQ.options.push({ key: key, content: content, isCorrect: true });
            }
            return;
        }

        // TRƯỜNG HỢP: Là đáp án thường (A., B., C., D.)
        if (str[1] === '.') {
            const key = firstChar;
            if (key >= 'A' && key <= 'Z') {
                const content = str.substring(2).trim();
                currentQ.options.push({ key: key, content: content, isCorrect: false });
                return;
            }
        }

        // TRƯỜNG HỢP: Dòng nối tiếp của câu hỏi
        currentQ.questionText += " <br>" + str;
    }

    // UPDATE DOM 1 LẦN DUY NHẤT
    previewDiv.innerHTML = htmlBuffer;

    // Gán lại sự kiện click cho các option sau khi render
    document.querySelectorAll('.option-item').forEach(item => {
        item.addEventListener('click', toggleAnswer);
    });

    console.timeEnd("UltraFastParse");
}

// 5. Tính năng TƯƠNG TÁC 1: Nhấn câu bên Xem trước -> Nhảy tới bên Nội dung thô
function scrollToRaw(rawStart) {
    if (!textArea) return;

    // 1. Focus và bôi đen vị trí (để con trỏ nhấp nháy đúng chỗ)
    textArea.focus();
    textArea.setSelectionRange(rawStart, rawStart);

    // 2. KỸ THUẬT MIRROR DIV: Đo chiều cao thực tế (bao gồm cả dòng wrap)
    // Tạo một div ảo để mô phỏng nội dung tính đến vị trí con trỏ
    const mirrorDiv = document.createElement('div');
    const style = window.getComputedStyle(textArea);

    // Sao chép các thuộc tính CSS quan trọng ảnh hưởng đến kích thước chữ và dòng
    const props = ['box-sizing', 'width', 'padding', 'border', 'font-family', 'font-size', 'font-weight', 'font-style', 'letter-spacing', 'line-height', 'text-transform', 'word-spacing', 'text-indent'];

    props.forEach(prop => {
        mirrorDiv.style[prop] = style.getPropertyValue(prop);
    });

    // Các style bắt buộc để div này ẩn đi nhưng vẫn đo được
    mirrorDiv.style.position = 'absolute';
    mirrorDiv.style.top = '0px';
    mirrorDiv.style.left = '-9999px'; // Giấu khỏi màn hình
    mirrorDiv.style.visibility = 'hidden';
    mirrorDiv.style.whiteSpace = 'pre-wrap'; // Quan trọng: Giữ định dạng xuống dòng giống textarea
    mirrorDiv.style.wordWrap = 'break-word';

    // Lấy nội dung từ đầu đến vị trí con trỏ
    const textUntilCursor = textArea.value.substring(0, rawStart);

    // Đưa nội dung vào div. Thêm một ký tự đặc biệt để giữ dòng cuối nếu là xuống dòng
    mirrorDiv.textContent = textUntilCursor;

    // Thêm div vào body để trình duyệt tính toán kích thước
    document.body.appendChild(mirrorDiv);

    // 3. Lấy chiều cao thực tế (Đây là vị trí pixel chính xác của dòng đó)
    const exactHeight = mirrorDiv.scrollHeight;

    // Xóa div ảo sau khi dùng xong
    document.body.removeChild(mirrorDiv);

    // 4. Tính toán vị trí cuộn để đưa dòng đó ra GIỮA màn hình
    // Công thức: Vị trí dòng - (1/2 chiều cao khung nhìn)
    const viewportHeight = textArea.clientHeight;

    // Dùng setTimeout để đảm bảo việc cuộn xảy ra sau khi trình duyệt focus xong
    setTimeout(() => {
        textArea.scrollTop = exactHeight - (viewportHeight / 2);
    }, 10);
}


// 6. Tính năng TƯƠNG TÁC 2: Chỉnh Đáp án bên Xem trước -> Sửa nội dung thô
function toggleAnswer(event) {
    const optionElement = event.currentTarget;
    const qIndex = parseInt(optionElement.getAttribute('data-q-index'));
    const key = optionElement.getAttribute('data-opt-key');

    if (isNaN(qIndex)) return;

    const currentQ = finalExamData[qIndex];
    if (!currentQ) return;

    const rawText = textArea.value;
    const isCurrentlyCorrect = optionElement.classList.contains('is-correct');

    let newRawText = rawText;

    // 1. TÌM VÀ GỠ BỎ ĐÁP ÁN ĐÚNG CŨ (Nếu có)
    if (currentQ.correctKey) {
        // Tạo chuỗi tìm kiếm đáp án đúng cũ: *<Key>.<Space>
        const oldCorrectKey = currentQ.correctKey;
        // Chuẩn hóa câu hỏi để tìm kiếm chính xác vị trí trong văn bản thô
        const questionTextWithoutPrefix = currentQ.questionText.replace(/<\s*br\s*>/g, ' ').trim();
        const questionStart = rawText.indexOf(questionTextWithoutPrefix.split('<br>')[0].split('\n')[0].trim());

        // Nếu không tìm thấy vị trí câu hỏi (do người dùng sửa quá nhiều), thì thoát
        if (questionStart === -1) {
            alert("Lỗi: Không thể tìm thấy vị trí câu hỏi trong nội dung thô để sửa.");
            return;
        }

        // Khu vực tìm kiếm (chỉ tìm trong phạm vi câu hỏi hiện tại)
        let searchArea = rawText.substring(questionStart);
        const nextQIndex = qIndex + 1;
        if (nextQIndex < finalExamData.length) {
            // Giới hạn khu vực tìm kiếm đến câu hỏi tiếp theo
            searchArea = rawText.substring(questionStart, finalExamData[nextQIndex].rawStart);
        }

        // Tìm vị trí của đáp án đúng cũ (*Key.) trong khu vực tìm kiếm
        const oldCorrectLinePrefix = `*${oldCorrectKey}.`;
        let oldCorrectLineIndexInSearchArea = searchArea.indexOf(oldCorrectLinePrefix);

        if (oldCorrectLineIndexInSearchArea !== -1) {
            // Vị trí tuyệt đối trong rawText
            const absoluteOldCorrectIndex = questionStart + oldCorrectLineIndexInSearchArea;

            // Thay thế '*' bằng khoảng trắng (biến *A. thành A.)
            newRawText = newRawText.substring(0, absoluteOldCorrectIndex) +
                newRawText.substring(absoluteOldCorrectIndex + 1);
        }
    }

    // 2. THÊM ĐÁP ÁN ĐÚNG MỚI (Nếu không phải là đang gỡ bỏ đáp án cũ)
    if (!isCurrentlyCorrect) {
        // Tạo chuỗi tìm kiếm đáp án mới: <Key>.<Space>
        const newCorrectLinePrefix = `${key}.`;

        // Cần phải tìm lại vị trí của câu hỏi trong newRawText
        const questionTextWithoutPrefix = currentQ.questionText.replace(/<\s*br\s*>/g, ' ').trim();
        const questionStartNew = newRawText.indexOf(questionTextWithoutPrefix.split('<br>')[0].split('\n')[0].trim());

        let searchAreaNew = newRawText.substring(questionStartNew);
        const nextQIndex = qIndex + 1;
        if (nextQIndex < finalExamData.length) {
            // Giới hạn khu vực tìm kiếm đến câu hỏi tiếp theo
            searchAreaNew = newRawText.substring(questionStartNew, finalExamData[nextQIndex].rawStart);
        }

        let newCorrectLineIndexInSearchArea = searchAreaNew.indexOf(newCorrectLinePrefix);

        if (newCorrectLineIndexInSearchArea !== -1) {
            // Vị trí tuyệt đối trong newRawText
            const absoluteNewCorrectIndex = questionStartNew + newCorrectLineIndexInSearchArea;

            // Chèn '*' vào trước (biến A. thành *A.)
            newRawText = newRawText.substring(0, absoluteNewCorrectIndex) +
                '*' + newRawText.substring(absoluteNewCorrectIndex);
        }
    }

    // 3. Cập nhật nội dung thô và kích hoạt parse lại
    textArea.value = newRawText;
    processText();


}


// 7. Hàm Lưu (Giống code cũ)
function saveExam() {
    if (finalExamData.length === 0) {
        alert("Chưa có dữ liệu đề thi!");
        return;
    }
    console.log("Dữ liệu gửi đi:", JSON.stringify(finalExamData));
    const My = document.getElementById('examModal');
    showExamModal()
    My.style.display = 'flex';
}
// Hàm ẩn lỗi
function clearErrors() {
    document.querySelectorAll('.error-message').forEach(el => {
        el.style.display = 'none';
        el.innerText = '';
    });
}

function showExamModal() {

    if (finalExamData.length === 0) {
        alert("Chưa có câu hỏi nào được tạo! Vui lòng nhập nội dung.");
        return;
    }
    const totalEl = document.getElementById('totalQuestionsCount');
    if (totalEl) {
        // Lấy độ dài mảng finalExamData
        totalEl.innerText = finalExamData.length;
    }
    document.getElementById('examModal').style.display = 'flex';
}

function closeModal() {
    document.getElementById('examModal').style.display = 'none';
    // Reset form
    document.getElementById('examTitle').value = '';
    document.getElementById('examDuration').value = '';
    document.querySelector('input[name="examType"][value="exam"]').checked = true;
    clearErrors();
}
window.showExamModal = showExamModal;
window.closeModal = closeModal;
