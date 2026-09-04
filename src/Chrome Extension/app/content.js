console.log("Chrome extension - CONTENT.JS LOADED");
chrome.runtime.onMessage.addListener((request, sender, sendResponse) => {

    if (request.action === "getDmcTitle") {

        const h1 = document.querySelector("div.header h1");

        if (!h1) {
            sendResponse({
                dmcTitle: ""
            });
            return;
        }

        const dmcTitle = Array.from(h1.childNodes)
            .filter(node => node.nodeType === Node.TEXT_NODE)
            .map(node => node.textContent)
            .join(" ")
            .replace(/\s+/g, " ")
            .trim();

        sendResponse({
            dmcTitle: dmcTitle
        });
    }

    return true;
});