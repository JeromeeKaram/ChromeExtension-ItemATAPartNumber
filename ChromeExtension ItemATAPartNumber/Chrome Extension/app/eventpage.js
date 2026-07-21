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
                    <td style="text-align:center;font-weight:700; color:#000;">${row.ItemNumber}</td>
                    <td style="text-align:center;">${row.PartNumber}</td>
                    <td>${row.PartDescription}</td>
		            <td>${row.Quantity}</td>
                </tr>
            `;
        });
}
else
{
tableRows = `
                <tr>
                    <td colspan="4" style="text-align:center; color:red;">
                        Part Numbers Not Found
                    </td>
                </tr>
            `;
}

        

        alert({
    html: `
<style>
    body {
        font-family: Arial, Helvetica, sans-serif;
        margin: 10px;
        background: #f7f7f7;
        color: #333;
    }

    .info {
        margin-bottom: 10px;
        font-size: 14px;
    }

    .info b {
        color: #111;
    }

    table {
        width: 100%;
        border-collapse: collapse;
        background: white;
        border-radius: 8px;
        overflow: hidden;
        box-shadow: 0 2px 6px rgba(0,0,0,0.1);
        font-size: 13px;
    }

    thead {
        background: #2f5bea;
        color: white;
    }

    th {
    padding: 10px;
    text-align: center;
    border-bottom: 1px solid #eee;
}

td {
    padding: 10px;
    text-align: left;
    border-bottom: 1px solid #eee;
}

    tbody tr:hover {
        background: #f1f6ff;
    }

    .not-found {
        color: #d9534f;
        font-weight: bold;
        padding: 12px;
    }
</style>

<div class="info">
    <b>Item Selected:</b> ${itemNumber}
</div>

<div class="info">
    <b>ATA Code:</b> ${ataCode}
</div>

<div style="text-align:center; margin-bottom:15px;">
    <img
        src="${result.base64Image}"
        alt="Diagram"
        style="
            max-width:100%;
            max-height:300px;
            border:1px solid #ccc;
            border-radius:6px;
            background:white;
        "
    />
</div>


<table>
    <thead>
        <tr>
            <th>Item Number</th>
            <th>Part Number</th>
            <th>Part Description</th>
	        <th>Quantity</th>
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

