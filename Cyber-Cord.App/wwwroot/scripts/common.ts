function getScrollPosition(elementId: string) {
    let element = document.getElementById(elementId);

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

function setScrollTop(elementId: string, scrollTop: number) {
    let element = document.getElementById(elementId);

    if (element == null) {
        return;
    }

    element.scrollTop = scrollTop;
}

function copyTextToClipboard(text: string) {
    navigator.clipboard.writeText(text)
        .then(function () {
            alert("Copied to clipboard!");
        })
        .catch(function (error) {
            alert(error);
        });
}

function pingMaxOpacity() {
    let element = document.getElementById("ping-bubble-id");

    element.style.transition = "opacity 0s linear"
    element.style.opacity = "1";
}

function pingMinOpacity() {
    let element = document.getElementById("ping-bubble-id");

    element.style.transition = "opacity 2s linear"
    element.style.opacity = "0";
}