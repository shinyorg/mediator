let handler;

window.InternetService = {
    subscribe: function(interop) {

        handler = function() {
            interop.invokeMethodAsync("InternetService.OnStatusChanged", navigator.onLine);
        }

        window.addEventListener("online", handler);
        window.addEventListener("offline", handler);
    },
    
    unsubscribe: function() {
        if (handler == null) 
            return;

        window.removeEventListener("online", handler);
        window.removeEventListener("offline", handler);
    }
};