(function () {
  try {
    var search = window.location.search;
    var rawReturnUrl = new URLSearchParams(search).get('returnUrl');
    if (!rawReturnUrl) return;

    var returnUrl;
    try {
      var parsedReturn = new URL(rawReturnUrl);
      if (parsedReturn.origin === window.location.origin) return;
      returnUrl = parsedReturn.toString();
    } catch {
      return;
    }

    var accessToken = localStorage.getItem('@Archon:accessToken');
    var refreshToken = localStorage.getItem('@Archon:refreshToken');
    if (!accessToken || !refreshToken) return;

    var payload;
    try {
      var base64Url = accessToken.split('.')[1];
      var base64 = base64Url.replace(/-/g, '+').replace(/_/g, '/');
      var jsonPayload = decodeURIComponent(
        atob(base64)
          .split('')
          .map(function (c) { return '%' + ('00' + c.charCodeAt(0).toString(16)).slice(-2); })
          .join('')
      );
      payload = JSON.parse(jsonPayload);
    } catch {
      return;
    }

    if (!payload || !payload.exp) return;
    var expiryTime = payload.exp * 1000;
    if (Date.now() >= expiryTime) return;

    var contractRaw = localStorage.getItem('@Archon:contract');
    if (!contractRaw) return;

    var contract;
    try {
      contract = JSON.parse(contractRaw);
    } catch {
      return;
    }

    var redirectUris = contract && contract.redirectUris;
    if (!redirectUris) return;

    var normalizeUrl = function (value) {
      var parsed = new URL(value);
      var path = parsed.pathname.replace(/\/+$/, '') || '/';
      return parsed.origin + path;
    };

    var normalizedReturnUrl = normalizeUrl(returnUrl);
    var allowed = redirectUris
      .split(',')
      .map(function (uri) { return uri.trim(); })
      .filter(Boolean)
      .flatMap(function (uri) {
        var normalizedUri = uri.replace(/\/+$/, '');
        return [normalizedUri, normalizedUri + '/callback'];
      })
      .some(function (uri) {
        try {
          return normalizeUrl(uri) === normalizedReturnUrl;
        } catch {
          return false;
        }
      });

    if (!allowed) return;

    var callbackUrl = new URL(returnUrl);
    callbackUrl.searchParams.set('accessToken', accessToken);
    callbackUrl.searchParams.set('refreshToken', refreshToken);
    window.location.replace(callbackUrl.toString());
  } catch {
    // ignore
  }
})();
