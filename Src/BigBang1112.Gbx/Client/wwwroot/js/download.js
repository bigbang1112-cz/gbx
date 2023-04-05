function download_file(fileName, content, mime) {
    const blob = new Blob([content], { type: mime });
    const link = document.createElement('a');

    link.href = window.URL.createObjectURL(blob);
    link.download = fileName;
    link.click();
}