var menuItemForItemNumber = {
    "id": "selectedItemNumber",
    "title": "Find ATA Code and PN",
    "contexts": ["selection"]
};

chrome.runtime.onInstalled.addListener(() => {
    chrome.contextMenus.removeAll(() => { 
        chrome.contextMenus.create(menuItemForItemNumber);
    });
});

chrome.contextMenus.onClicked.addListener(function (clickData, tab) {

    try {

        if (clickData.menuItemId === "selectedItemNumber" &&
            clickData.selectionText) {

            const itemNumber = clickData.selectionText.trim();

            // current page URL
            const pageUrl = tab.url;

            // API URL
            const url = new URL(
                "http://localhost/ChromeExtItemATAPartNumber/ItemATAPartNumber/ATAPNAsync"
            );

            url.searchParams.append("itemNumber", itemNumber);
            url.searchParams.append("pageUrl", pageUrl);

            fetch(url)
                .then(response => response.json())
                .then(result => {

                     

                    let ata = result.ata;
    		    let pn = result.pn;
                    let itemNo = result.itemNumber;

		    console.log(itemNo);
    		    alert({
    html:  `
            <b>ATA</b>: ${ata}<br>
            <b>Part Number</b>: ${pn}<br>
            <b>Item Number</b>: ${itemNo}
        `
});

                })
                .catch(err => {

                    alert("API Error: " + err.message);

                });
        }

    }
    catch (e) {

        alert("Error " + e);

    }
});


chrome.storage.onChanged.addListener(function (changes, storageName) {
    if (
        changes &&
        changes.total &&
        changes.total.newValue != null
    ) {

        chrome.action.setBadgeText({
            text: String(changes.total.newValue)
        });

    }
});



async function alert({
    html,
    title = chrome.runtime.getManifest().name,
    width = 600,
    height = 400,
    left,
    top,
}) {
    const w = left == null && top == null && await chrome.windows.getCurrent();
    const w2 = await chrome.windows.create({
        url: `data:text/html,<title>${title}</title>${html}`.replace(/#/g, '%23'),
        type: 'popup',
        left: left ?? Math.floor(w.left + (w.width - width) / 2),
        top: top ?? Math.floor(w.top + (w.height - height) / 2),
        height,
        width,
    });
    return new Promise(resolve => {
        chrome.windows.onRemoved.addListener(onRemoved, { windowTypes: ['popup'] });
        function onRemoved(id) {
            if (id === w2.id) {
                chrome.windows.onRemoved.removeListener(onRemoved);
                resolve();
            }
        }
    });
}

