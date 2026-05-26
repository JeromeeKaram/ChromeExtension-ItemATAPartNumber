chrome.runtime.onInstalled.addListener(() => {
    chrome.contextMenus.removeAll(() => { 

// Parent menu (this becomes the “extension name group”)
        chrome.contextMenus.create({
            id: "rootMenu",
            title: "Chrome Extension - EIPD",
            contexts: ["all"]
        });

        // Child menu (your actual action)
        chrome.contextMenus.create({
            id: "selectedItemNumber",
            parentId: "rootMenu",
            title: "Find ATA Code && Part Number",
            contexts: ["selection"]
        });        

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

if (result.partNumbers && result.partNumbers.length > 0)
{
result.partNumbers.forEach(row => {
            tableRows += `
                <tr>
                    <td style="text-align:center;">${row.ItemNumber}</td>
                    <td style="text-align:center;">${row.PartNumber}</td>
                    <td style="text-align:center;">${row.PartDescription}</td>
                </tr>
            `;
        });
}
else
{
tableRows = `
                <tr>
                    <td colspan="3" style="text-align:center; color:red;">
                        Part Numbers Not Found
                    </td>
                </tr>
            `;
}

        

        alert({
            html: `
<div style="margin-bottom:5px;">
        <b>Item No Selected:</b> ${itemNumber}
    </div>

    <div style="margin-bottom:10px;">
        <b>ATA Code:</b> ${ataCode}
    </div>
                <table border="1" cellpadding="5" cellspacing="0" style="border-collapse:collapse; width:100%;">
                    <thead>
                        <tr>
                            <th style="text-align:center;">Item Number</th>
                            <th style="text-align:center;">Part Number</th>
                            <th style="text-align:center;">Part Description</th>
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

