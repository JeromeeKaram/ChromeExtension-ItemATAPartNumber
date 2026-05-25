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

	    const fileName = pageUrl.split('/').pop();
	
	    const arraySplitByDash = fileName.split('-');

	    const ataCode = arraySplitByDash[2] + arraySplitByDash[3] + arraySplitByDash[4];

            // API URL
            const url = new URL(
                "http://localhost/ChromeExtItemATAPartNumber/ItemATAPartNumber/ATAPNAsync"
            );

            url.searchParams.append("itemNumber", itemNumber);
            url.searchParams.append("pageUrl", pageUrl);

            fetch(url)
                .then(response => response.json())
                .then(result => {

                    console.log(result);

        let tableRows = "";

        result.partNumbers.forEach(row => {
            tableRows += `
                <tr>
                    <td>${row.ItemNumber}</td>
                    <td>${row.PartNumber}</td>
                </tr>
            `;
        });

        alert({
            html: `
<div style="margin-bottom:10px;">
            <b>ATA Code:</b> ${ataCode}
        </div>
                <table border="1" cellpadding="5" cellspacing="0" style="border-collapse:collapse; width:100%;">
                    <thead>
                        <tr>
                            <th>Item Number</th>
                            <th>Part Number</th>
                        </tr>
                    </thead>
                    <tbody>
                        ${tableRows}
                    </tbody>
                </table>
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

