window.floatingWindow = (() => {

    function init(windowId, titlebarId, resizeHandleId, dotnetRef) {
        const win       = document.getElementById(windowId);
        const titlebar  = document.getElementById(titlebarId);
        const resizeHandle = document.getElementById(resizeHandleId);

        if (!win || !titlebar) return;

        // ----------------------------------------------------------------
        // Drag
        // ----------------------------------------------------------------
        let isDragging = false;
        let dragOffsetX = 0;
        let dragOffsetY = 0;

        titlebar.addEventListener('mousedown', (e) => {
            if (e.target.closest('.call-controls')) return; // don't drag on buttons
            isDragging  = true;
            dragOffsetX = e.clientX - win.getBoundingClientRect().left;
            dragOffsetY = e.clientY - win.getBoundingClientRect().top;
            win.style.transition = 'none';
            e.preventDefault();
        });

        document.addEventListener('mousemove', (e) => {
            if (!isDragging) return;

            let newX = e.clientX - dragOffsetX;
            let newY = e.clientY - dragOffsetY;

            // Clamp to viewport
            newX = Math.max(0, Math.min(newX, window.innerWidth  - win.offsetWidth));
            newY = Math.max(0, Math.min(newY, window.innerHeight - win.offsetHeight));

            win.style.left = newX + 'px';
            win.style.top  = newY + 'px';
        });

        document.addEventListener('mouseup', (e) => {
            if (!isDragging) return;
            isDragging = false;
            const rect = win.getBoundingClientRect();
            dotnetRef.invokeMethodAsync('UpdatePosition', rect.left, rect.top);
        });

        // ----------------------------------------------------------------
        // Resize
        // ----------------------------------------------------------------
        if (!resizeHandle) return;

        let isResizing  = false;
        let resizeStartX = 0;
        let resizeStartY = 0;
        let resizeStartW = 0;
        let resizeStartH = 0;

        resizeHandle.addEventListener('mousedown', (e) => {
            isResizing   = true;
            resizeStartX = e.clientX;
            resizeStartY = e.clientY;
            resizeStartW = win.offsetWidth;
            resizeStartH = win.offsetHeight;
            win.style.transition = 'none';
            e.preventDefault();
            e.stopPropagation();
        });

        document.addEventListener('mousemove', (e) => {
            if (!isResizing) return;
            const newW = Math.max(260, resizeStartW + (e.clientX - resizeStartX));
            const newH = Math.max(300, resizeStartH + (e.clientY - resizeStartY));
            win.style.width  = newW + 'px';
            win.style.height = newH + 'px';
        });

        document.addEventListener('mouseup', () => {
            if (!isResizing) return;
            isResizing = false;
            dotnetRef.invokeMethodAsync('UpdateSize', win.offsetWidth, win.offsetHeight);
        });

        // ----------------------------------------------------------------
        // Touch support (mobile drag)
        // ----------------------------------------------------------------
        titlebar.addEventListener('touchstart', (e) => {
            if (e.target.closest('.call-controls')) return;
            const touch = e.touches[0];
            isDragging  = true;
            dragOffsetX = touch.clientX - win.getBoundingClientRect().left;
            dragOffsetY = touch.clientY - win.getBoundingClientRect().top;
        }, { passive: true });

        document.addEventListener('touchmove', (e) => {
            if (!isDragging) return;
            const touch = e.touches[0];
            let newX = touch.clientX - dragOffsetX;
            let newY = touch.clientY - dragOffsetY;
            newX = Math.max(0, Math.min(newX, window.innerWidth  - win.offsetWidth));
            newY = Math.max(0, Math.min(newY, window.innerHeight - win.offsetHeight));
            win.style.left = newX + 'px';
            win.style.top  = newY + 'px';
        }, { passive: true });

        document.addEventListener('touchend', () => {
            if (!isDragging) return;
            isDragging = false;
            const rect = win.getBoundingClientRect();
            dotnetRef.invokeMethodAsync('UpdatePosition', rect.left, rect.top);
        });
    }

    function initResize(windowId, resizeHandleId, dotnetRef) {
        const win = document.getElementById(windowId);
        const resizeHandle = document.getElementById(resizeHandleId);
        if (!win || !resizeHandle) return;

        // Clone the handle to strip old listeners
        const newHandle = resizeHandle.cloneNode(true);
        resizeHandle.parentNode.replaceChild(newHandle, resizeHandle);

        let isResizing  = false;
        let resizeStartX = 0;
        let resizeStartY = 0;
        let resizeStartW = 0;
        let resizeStartH = 0;

        newHandle.addEventListener('mousedown', (e) => {
            isResizing   = true;
            resizeStartX = e.clientX;
            resizeStartY = e.clientY;
            resizeStartW = win.offsetWidth;
            resizeStartH = win.offsetHeight;
            win.style.transition = 'none';
            e.preventDefault();
            e.stopPropagation();
        });

        document.addEventListener('mousemove', (e) => {
            if (!isResizing) return;
            const newW = Math.max(260, resizeStartW + (e.clientX - resizeStartX));
            const newH = Math.max(300, resizeStartH + (e.clientY - resizeStartY));
            win.style.width  = newW + 'px';
            win.style.height = newH + 'px';
        });

        document.addEventListener('mouseup', () => {
            if (!isResizing) return;
            isResizing = false;
            dotnetRef.invokeMethodAsync('UpdateSize', win.offsetWidth, win.offsetHeight);
        });
    }
    
    return { init, initResize };

})();
