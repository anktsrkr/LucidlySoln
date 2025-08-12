
window.scrollToBottom = (element) => {
        if (element) {
        // Use requestAnimationFrame for smoother scrolling, especially during rapid updates
        requestAnimationFrame(() => {
            element.scrollTop = element.scrollHeight;
        });
        } else {
        console.warn("scrollToBottom called with null element");
        }
    };

    // Optional: Fallback global scroll to bottom of .chat-container if element ref fails
    // window.scrollToBottomGlobal = () => {
    //     const container = document.querySelector('.chat-container');
    //     if (container) {
    //         container.scrollTop = container.scrollHeight;
    //     }
    // };