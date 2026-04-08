function getScrollPosition(elementId) {
    var element = document.getElementById(elementId);
    if (element == null) {
        return null;
    }
    return {
        scrollTop: element.scrollTop,
        scrollLeft: element.scrollLeft,
        scrollHeight: element.scrollHeight,
        clientHeight: element.clientHeight,
    };
}
function setScrollTop(elementId, scrollTop) {
    var element = document.getElementById(elementId);
    if (element == null) {
        return;
    }
    element.scrollTop = scrollTop;
}
function copyTextToClipboard(text) {
    navigator.clipboard.writeText(text)
        .then(function () {
        alert("Copied to clipboard!");
    })
        .catch(function (error) {
        alert(error);
    });
}
function pingMaxOpacity() {
    var element = document.getElementById("ping-bubble-id");
    element.style.transition = "opacity 0s linear";
    element.style.opacity = "1";
}
function pingMinOpacity() {
    var element = document.getElementById("ping-bubble-id");
    element.style.transition = "opacity 2s linear";
    element.style.opacity = "0";
}
//# sourceMappingURL=common.js.map