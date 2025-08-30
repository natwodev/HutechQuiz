// JavaScript functions for Lecturer Dashboard

// Function to download file
window.downloadFile = function (fileName, content) {
    const blob = new Blob([content], { type: 'text/csv;charset=utf-8;' });
    const link = document.createElement('a');
    
    if (link.download !== undefined) {
        const url = URL.createObjectURL(blob);
        link.setAttribute('href', url);
        link.setAttribute('download', fileName);
        link.style.visibility = 'hidden';
        document.body.appendChild(link);
        link.click();
        document.body.removeChild(link);
    }
};

// Function to show confirmation dialog
window.confirm = function (message) {
    return new Promise((resolve) => {
        const result = window.confirm(message);
        resolve(result);
    });
};

// Function to show prompt dialog
window.prompt = function (message) {
    return new Promise((resolve) => {
        const result = window.prompt(message);
        resolve(result);
    });
};

// Function to show notification
window.showNotification = function (message, type = 'info') {
    // You can implement this using any notification library
    // For now, using simple alert
    alert(message);
};

// Function to format date
window.formatDate = function (dateString) {
    if (!dateString) return '-';
    const date = new Date(dateString);
    return date.toLocaleDateString('vi-VN') + ' ' + date.toLocaleTimeString('vi-VN');
};

// Function to refresh page
window.refreshPage = function () {
    window.location.reload();
};

// Function to go back
window.goBack = function () {
    window.history.back();
};

// Function to download Excel file
window.downloadExcelFile = function (fileName, base64Content) {
    try {
        // Convert base64 to binary
        const binaryString = atob(base64Content);
        const bytes = new Uint8Array(binaryString.length);
        for (let i = 0; i < binaryString.length; i++) {
            bytes[i] = binaryString.charCodeAt(i);
        }
        
        // Create blob with Excel MIME type
        const blob = new Blob([bytes], { 
            type: 'application/vnd.openxmlformats-officedocument.spreadsheetml.sheet' 
        });
        
        // Create download link
        const link = document.createElement('a');
        const url = URL.createObjectURL(blob);
        link.setAttribute('href', url);
        link.setAttribute('download', fileName);
        link.style.visibility = 'hidden';
        
        // Trigger download
        document.body.appendChild(link);
        link.click();
        document.body.removeChild(link);
        
        // Clean up
        URL.revokeObjectURL(url);
    } catch (error) {
        console.error('Error downloading Excel file:', error);
        alert('Lỗi khi tải file Excel: ' + error.message);
    }
}; 