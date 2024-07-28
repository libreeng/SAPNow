function isLoggedIn() {
    return Boolean(sessionStorage.getItem("token"));
}

async function idpLogin(auth) {
    const cloudHost = sessionStorage.getItem("cloudHost");
    const accountId = sessionStorage.getItem("accountId");
    const accountName = sessionStorage.getItem("accountName");
    const companyId = sessionStorage.getItem("companyId");
    const userId = sessionStorage.getItem("userId");

    try {
        const response = await fetch('/auth/', {
            method: 'GET',
            headers: {
                'Content-Type': 'application/json',
                'Authorization': 'Bearer ' + auth.access_token,
                'X-Cloud-Host': cloudHost,
                'X-Account-Name': accountName,
                'X-Company-ID': companyId,
                'X-User-ID': userId
            }
        });
        if (response.status === 200) {
            const data = await response.json();
            if (data.token) {
                sessionStorage.setItem("token", data.token);
                sessionStorage.setItem("fromEmail", data.email);
                console.log(data.token);
            } else {
                console.log("Misconfigured!");
            }
        }
    }
    catch (ex) {
        console.log(ex.message || 'An unknown error occured');
    }
}
