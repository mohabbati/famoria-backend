window.popupIsClosed = function(popup) {
    try {
        return popup.closed || typeof popup.closed === 'undefined';
    } catch (e) {
        // If we can't access the popup object, it's likely closed
        return true;
    }
};
