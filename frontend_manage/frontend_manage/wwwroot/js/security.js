
// Ngăn người dùng nhấn tổ hợp phím F12
document.addEventListener('keydown', function (event) {
    if (event.key === "F12" ||
        (event.ctrlKey && event.shiftKey && event.key === "I") ||
        (event.ctrlKey && event.shiftKey && event.key === "J") ||
        (event.ctrlKey && event.key === "U")) {
        event.preventDefault();
    }
});

//Ngăn người dùng nhấp menu chuột phải
document.addEventListener('contextmenu', function (event) {
    event.preventDefault();
});
