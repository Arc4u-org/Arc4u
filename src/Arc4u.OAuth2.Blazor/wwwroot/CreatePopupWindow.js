

window.Arc4u = {
    CreatePopupWindow: function (authority, feature) {

        const win = window.open("_content/Arc4u.OAuth2.Blazor/Authenticate.html", "_blank", feature);

        if (!win || win.closed || typeof win.closed == "undefined") {
            window.location.href = "popup.html";
            return;

        };

        win.onload = function () {
            win.location.href = authority;
        }
    }
};
