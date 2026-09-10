window.hd2App = {
  getLocalStorage(key) {
    return window.localStorage.getItem(key);
  },
  setLocalStorage(key, value) {
    window.localStorage.setItem(key, value);
  },
  removeLocalStorage(key) {
    window.localStorage.removeItem(key);
  },
  downloadFile(fileName, contentType, content) {
    const blob = new Blob([content], { type: contentType });
    const url = URL.createObjectURL(blob);
    const anchor = document.createElement("a");
    anchor.href = url;
    anchor.download = fileName;
    document.body.appendChild(anchor);
    anchor.click();
    document.body.removeChild(anchor);
    URL.revokeObjectURL(url);
  }
};
