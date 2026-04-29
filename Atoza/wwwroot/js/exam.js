
// Các biến toàn cục (sẽ được khởi tạo từ file .cshtml)
// Lưu ý: Không dùng let/var để khai báo lại totalQuestions/totalSeconds ở đây 
// vì chúng đã được tạo ra bởi script inline trong View (Razor).

// Các phần tử DOM
const timerElement = document.getElementById('countdown');
const timerBox = document.getElementById('timerBox');
const modalTimer = document.getElementById('modal-time-left');
const answeredCountElement = document.getElementById('answered-count');
const totalCountElement = document.getElementById('total-count');
const examForm = document.getElementById('examForm');

// Khai báo biến interval để có thể xóa bỏ sau này
let interval;
let answered;

/**
 * Cập nhật số lượng câu hỏi đã trả lời và hiển thị trên modal/sidebar.
 */
function updateAnsweredCount() {
    answered = document.querySelectorAll('.p-item.answered').length;
    if (answeredCountElement) answeredCountElement.innerText = answered;
    if (totalCountElement) {
        // Fallback nếu totalQuestions chưa được định nghĩa
        const total = (typeof totalQuestions !== 'undefined') ? totalQuestions : document.querySelectorAll('.question-card').length;
        totalCountElement.innerText = total;
    }
}

/**
 * Định dạng thời gian từ giây sang chuỗi "MM:SS".
 * @param {number} seconds - Số giây còn lại.
 * @returns {string}
 */
function formatTime(seconds) {
    const m = Math.floor(seconds / 60);
    const s = seconds % 60;
    return (m < 10 ? '0' + m : m) + ':' + (s < 10 ? '0' + s : s);
}

/**
 * Bắt đầu đếm ngược thời gian làm bài.
 */
function startCountdown() {
    if (typeof totalSeconds === 'undefined') return;

    if (totalSeconds <= 0) {
        if (timerElement) timerElement.innerText = "00:00";
        // Nếu thời gian ban đầu <= 0, tự động nộp bài ngay (nếu cần)
        if (totalSeconds < 0 && examForm) examForm.submit();
        return;
    }

    // Hiển thị thời gian ban đầu ngay lập tức
    if (timerElement) timerElement.innerText = formatTime(totalSeconds);
    if (modalTimer) modalTimer.innerText = formatTime(totalSeconds);
    if (totalSeconds < 300 && timerBox) timerBox.classList.add('danger');

    interval = setInterval(function () {
        totalSeconds--;
        const timeStr = formatTime(totalSeconds);

        if (timerElement) timerElement.innerText = timeStr;
        if (modalTimer) modalTimer.innerText = timeStr;

        if (totalSeconds < 300 && timerBox) timerBox.classList.add('danger');

        if (totalSeconds <= 0) {
            clearInterval(interval);
            // Gọi hàm AJAX nộp bài thay vì submit form thông thường
            if (typeof autoSubmitExam === 'function') {
                autoSubmitExam();
            } else if (typeof submitExamAjax === 'function') {
                alert("Hết giờ làm bài! Hệ thống đang tự động nộp.");
                submitExamAjax();
            }
        }
    }, 1000);
}

/**
 * @param {number} orderNum - Số thứ tự của câu hỏi.
 */
function markAnswered(orderNum) {
    const gridItem = document.getElementById('grid-item-' + orderNum);
    if (gridItem) gridItem.classList.add('answered');
    updateAnsweredCount();
}

function handleAnswerChange(qId, orderNum) {
    markAnswered(orderNum);

    if (typeof isPracticeMode !== 'undefined' && isPracticeMode) {
        checkPracticeAnswer(qId);
    }
}

function checkPracticeAnswer(qId) {
    const examIdElement = document.getElementById('examId');
    const selectedRadio = document.querySelector('input[name="q_' + qId + '"]:checked');
    if (!examIdElement || !selectedRadio) return;

    clearPracticeState(qId);
    const feedback = document.getElementById('practice-feedback-' + qId);
    if (feedback) {
        feedback.className = 'practice-feedback is-loading';
        feedback.innerText = 'Dang kiem tra dap an...';
    }

    fetch('/Exam/CheckPracticeAnswer', {
        method: 'POST',
        headers: {
            'Content-Type': 'application/json',
            'RequestVerificationToken': document.querySelector('input[name="__RequestVerificationToken"]')?.value || ''
        },
        body: JSON.stringify({
            ExamId: parseInt(examIdElement.value),
            QuestionId: qId,
            SelectedOption: selectedRadio.value
        })
    })
        .then(function (response) {
            return response.json().then(function (data) {
                return { ok: response.ok, data: data };
            });
        })
        .then(function (result) {
            const data = result.data;
            if (!result.ok || !data.success) {
                if (feedback) {
                    feedback.className = 'practice-feedback is-error';
                    feedback.innerText = data.message || 'Khong the kiem tra dap an.';
                }
                return;
            }

            showPracticeFeedback(qId, selectedRadio.value, data.correctAnswer, data.isCorrect);
        })
        .catch(function () {
            if (feedback) {
                feedback.className = 'practice-feedback is-error';
                feedback.innerText = 'Khong the ket noi de kiem tra dap an.';
            }
        });
}

function showPracticeFeedback(qId, selectedOption, correctAnswer, isCorrect) {
    clearPracticeState(qId);

    const selectedInput = document.querySelector('input[name="q_' + qId + '"][value="' + selectedOption + '"]');
    const correctInput = document.querySelector('input[name="q_' + qId + '"][value="' + correctAnswer + '"]');
    const selectedLabel = selectedInput ? selectedInput.nextElementSibling : null;
    const correctLabel = correctInput ? correctInput.nextElementSibling : null;

    if (correctLabel) correctLabel.classList.add('practice-correct');
    if (!isCorrect && selectedLabel) selectedLabel.classList.add('practice-incorrect');

    const feedback = document.getElementById('practice-feedback-' + qId);
    if (feedback) {
        feedback.className = 'practice-feedback ' + (isCorrect ? 'is-correct' : 'is-incorrect');
        feedback.innerHTML = isCorrect
            ? '<i class="fa-solid fa-circle-check me-1"></i> Chinh xac.'
            : '<i class="fa-solid fa-circle-xmark me-1"></i> Chua dung. Dap an dung: <strong>' + correctAnswer + '</strong>';
    }
}

function clearPracticeState(qId) {
    document.querySelectorAll('input[name="q_' + qId + '"] + .option-label').forEach(function (label) {
        label.classList.remove('practice-correct', 'practice-incorrect');
    });

    const feedback = document.getElementById('practice-feedback-' + qId);
    if (feedback) {
        feedback.className = 'practice-feedback';
        feedback.innerText = '';
    }
}

/**
 * @param {number} orderNum - Số thứ tự của câu hỏi.
 */
function toggleFlag(orderNum) {
    const btn = document.getElementById('btn-flag-' + orderNum);
    const gridItem = document.getElementById('grid-item-' + orderNum);
    if (!btn) return;

    btn.classList.toggle('active');

    const isFlagged = btn.classList.contains('active');

    const icon = btn.querySelector('i');
    if (icon) {
        if (isFlagged) {
            icon.classList.remove('fa-regular');
            icon.classList.add('fa-solid');
        } else {
            icon.classList.remove('fa-solid');
            icon.classList.add('fa-regular');
        }
    }

    if (gridItem) {
        gridItem.classList.toggle('flagged', isFlagged);

        // Thêm/Xóa icon cờ trong p-item
        let flagIcon = gridItem.querySelector('.flag-icon');
        if (isFlagged) {
            if (!flagIcon) {
                const iconEl = document.createElement('i');
                iconEl.className = 'fa-solid fa-flag flag-icon';
                gridItem.appendChild(iconEl);
            }
        } else {
            if (flagIcon) {
                gridItem.removeChild(flagIcon);
            }
        }
    }
}

/**
 * @param {number} qId - ID của câu hỏi.
 * @param {number} orderNum - Số thứ tự của câu hỏi.
 */
function clearAnswer(qId, orderNum) {
    const radios = document.getElementsByName('q_' + qId);
    for (let i = 0; i < radios.length; i++) {
        radios[i].checked = false;
    }

    clearPracticeState(qId);

    const gridItem = document.getElementById('grid-item-' + orderNum);
    if (gridItem) gridItem.classList.remove('answered');

    updateAnsweredCount();
}

/**
 * Hiển thị modal xác nhận nộp bài.
 */
function confirmSubmit() {
    updateAnsweredCount();
    // Cập nhật lại thời gian trong modal lần cuối trước khi hiển thị
    if (modalTimer && typeof totalSeconds !== 'undefined') {
        modalTimer.innerText = formatTime(totalSeconds);
    }
    const myModal = new bootstrap.Modal(document.getElementById('confirmSubmitModal'));
    const modalElement = document.getElementById('confirmSubmitModal');
    const answeredSubmit = modalElement.querySelector('#answered-count');
    if (answeredSubmit) answeredSubmit.innerText = answered;
    myModal.show();
}

/**
 * Cuộn đến vị trí của câu hỏi.
 * @param {number} num - Số thứ tự của câu hỏi.
 */
function scrollToQuestion(num) {
    const el = document.getElementById('q-card-' + num);
    if (el) {
        el.scrollIntoView({ behavior: 'smooth', block: 'start' });
        // Thêm hiệu ứng flash để người dùng dễ nhận biết
        el.classList.remove('highlight-flash'); // Reset animation
        void el.offsetWidth; // Trigger reflow
        el.classList.add('highlight-flash');
    }
}

/**
 * Nhảy tới câu hỏi dựa trên số thứ tự nhập vào (OrderNumber).
 * @param {string} numStr - Số thứ tự câu hỏi (dạng chuỗi).
 */
function jumpToQuestion(numStr) {
    if (!numStr) return;

    let num = parseInt(numStr);
    if (isNaN(num)) return;

    // Kiểm tra xem câu hỏi có tồn tại trong DOM không
    const targetCard = document.getElementById('q-card-' + num);

    if (targetCard) {
        scrollToQuestion(num);

        // Cập nhật lại giá trị trong ô input cho khớp (nếu cần)
        const inputElement = document.getElementById('question-jump-input');
        if (inputElement) {
            inputElement.value = num;
        }
    } else {
        alert("Không tìm thấy câu hỏi số " + num);
    }
}

// --- XỬ LÝ SỰ KIỆN DOM CONTENT LOADED ---
document.addEventListener("DOMContentLoaded", function () {
    // 1. Khởi chạy đồng hồ và cập nhật số câu hỏi
    startCountdown();
    updateAnsweredCount();

    // Xử lý phím Enter cho ô nhập số câu hỏi
    const jumpInput = document.getElementById('question-jump-input');
    if (jumpInput) {
        jumpInput.addEventListener('keydown', function (e) {
            if (e.key === 'Enter') {
                e.preventDefault();
                jumpToQuestion(this.value);
                this.blur();
            }
        });
    }

    // 2. Thiết lập Intersection Observer cho highlight câu hỏi đang xem
    let observer = new IntersectionObserver((entries) => {
        // Tìm câu hỏi đang hiển thị rõ nhất (intersectionRatio > 0, ưu tiên câu hỏi ở gần giữa)
        let bestEntry = null;
        let maxRatio = 0;

        entries.forEach(entry => {
            if (entry.isIntersecting && entry.intersectionRatio > maxRatio) {
                maxRatio = entry.intersectionRatio;
                bestEntry = entry;
            }
        });

        if (bestEntry) {
            const index = bestEntry.target.getAttribute('data-index');

            // Xóa highlight cũ
            document.querySelectorAll('.p-item').forEach(i => i.classList.remove('viewing'));
            document.querySelectorAll('.question-card').forEach(c => c.classList.remove('active-view'));

            // Thêm highlight mới
            const gridItem = document.getElementById('grid-item-' + index);
            if (gridItem) gridItem.classList.add('viewing');
            bestEntry.target.classList.add('active-view');
        }

    }, {
        // Căn lề để chỉ đánh dấu câu hỏi khi nó nằm trong khoảng giữa màn hình
        rootMargin: '-30% 0px -50% 0px'
    });

    // Bắt đầu quan sát tất cả các thẻ câu hỏi
    document.querySelectorAll('.question-card').forEach(card => observer.observe(card));
});

/**
 * Nộp bài thi qua AJAX đến endpoint /Exam/SubmitExam
 */
function submitExamAjax() {
    // 1. Lấy ExamId từ input hidden
    var examIdElement = document.getElementById('examId');
    if (!examIdElement) {
        alert("Lỗi: Không tìm thấy thông tin đề thi.");
        return;
    }
    var examId = parseInt(examIdElement.value);

    // 2. Thu thập tất cả câu trả lời
    var answers = [];
    var questionCards = document.querySelectorAll('.question-card');

    questionCards.forEach(function (card) {
        // Tìm radio button được chọn trong card này
        var selectedRadio = card.querySelector('input[type="radio"]:checked');
        if (selectedRadio) {
            // Lấy QuestionId từ name (format: q_123)
            var name = selectedRadio.name; // "q_123"
            var questionId = parseInt(name.replace('q_', ''));
            var selectedOption = selectedRadio.value; // "A", "B", "C", "D"

            answers.push({
                QuestionId: questionId,
                SelectedOption: selectedOption
            });
        }
    });

    // 3. Tạo object submission
    var submission = {
        ExamId: examId,
        Answers: answers
    };

    // 4. Hiển thị modal loading
    var loadingModal = document.getElementById('loadingModal');
    var loadingModalInstance = null;
    if (loadingModal) {
        loadingModalInstance = new bootstrap.Modal(loadingModal);
        loadingModalInstance.show();
    }

    // 5. Đóng modal xác nhận
    var confirmModal = document.getElementById('confirmSubmitModal');
    if (confirmModal) {
        var confirmModalInstance = bootstrap.Modal.getInstance(confirmModal);
        if (confirmModalInstance) confirmModalInstance.hide();
    }

    // 6. Gửi AJAX request
    fetch('/Exam/SubmitExam', {
        method: 'POST',
        headers: {
            'Content-Type': 'application/json',
            'RequestVerificationToken': document.querySelector('input[name="__RequestVerificationToken"]')?.value || ''
        },
        body: JSON.stringify(submission)
    })
        .then(function (response) {
            return response.json();
        })
        .then(function (data) {
            // Ẩn loading modal
            if (loadingModalInstance) loadingModalInstance.hide();

            if (data.success) {
                // Dừng đồng hồ đếm ngược
                if (typeof interval !== 'undefined') clearInterval(interval);

                // Thông báo điểm và chuyển hướng
                alert("Nộp bài thành công! Điểm của bạn: " + data.score + "/10");
                if (data.redirectUrl) {
                    window.location.href = data.redirectUrl;
                }
            } else {
                alert("Lỗi: " + data.message);
            }
        })
        .catch(function (error) {
            // Ẩn loading modal
            if (loadingModalInstance) loadingModalInstance.hide();
            alert("Lỗi kết nối: " + error.message);
            console.error("Submit error:", error);
        });
}

/**
 * Tự động nộp bài khi hết thời gian
 */
function autoSubmitExam() {
    alert("Hết giờ làm bài! Hệ thống đang tự động nộp.");
    submitExamAjax();
}

// Xuất các hàm cần dùng trong HTML (onclick, onchange) ra khỏi phạm vi module
window.markAnswered = markAnswered;
window.handleAnswerChange = handleAnswerChange;
window.toggleFlag = toggleFlag;
window.clearAnswer = clearAnswer;
window.confirmSubmit = confirmSubmit;
window.scrollToQuestion = scrollToQuestion;
window.jumpToQuestion = jumpToQuestion;
window.submitExamAjax = submitExamAjax;
window.autoSubmitExam = autoSubmitExam;
