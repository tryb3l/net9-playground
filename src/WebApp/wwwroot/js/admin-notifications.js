document.addEventListener('DOMContentLoaded', function () {
    const notificationElement = document.getElementById('tempDataSuccessMessage');
    if (notificationElement && notificationElement.dataset.message) {
        alert(notificationElement.dataset.message);
    }
});