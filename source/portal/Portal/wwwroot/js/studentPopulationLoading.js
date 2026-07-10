(function ($) {
    'use strict';

    var overlayId = 'sp-loading-overlay';

    function ensureOverlay() {
        if (document.getElementById(overlayId)) {
            return;
        }

        var style = document.createElement('style');
        style.textContent =
            '#' + overlayId + ' {' +
            '  position: fixed;' +
            '  inset: 0;' +
            '  z-index: 2000;' +
            '  display: none;' +
            '  align-items: center;' +
            '  justify-content: center;' +
            '  background: rgba(0, 0, 0, 0.45);' +
            '  pointer-events: auto;' +
            '}' +
            '#' + overlayId + ' .sp-loading-box {' +
            '  display: flex;' +
            '  flex-direction: column;' +
            '  align-items: center;' +
            '  gap: 12px;' +
            '  color: #fff;' +
            '  font-size: 16px;' +
            '}';
        document.head.appendChild(style);

        var overlay = document.createElement('div');
        overlay.id = overlayId;
        overlay.innerHTML =
            '<div class="sp-loading-box">' +
            '<div class="spinner-border text-light" role="status">' +
            '<span class="visually-hidden">Loading...</span>' +
            '</div>' +
            '<div>處理中，請稍候...</div>' +
            '</div>';
        document.body.appendChild(overlay);
    }

    function showOverlay() {
        ensureOverlay();
        document.getElementById(overlayId).style.display = 'flex';
    }

    function hideOverlay() {
        var overlay = document.getElementById(overlayId);
        if (overlay) {
            overlay.style.display = 'none';
        }
    }

    $(document).ready(ensureOverlay);
    $(document).ajaxStart(showOverlay);
    $(document).ajaxStop(hideOverlay);
})(jQuery);
