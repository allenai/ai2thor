mergeInto(LibraryManager.library, {

  Init: function () {
    // window.alert("Init test!");
    if (window.onGameLoaded && typeof(window.onGameLoaded) === 'function') {
        window.onGameLoaded();
    }
  },
  
  SendEvent: function(str) {
    if (window.onUnityEvent && typeof window.onUnityEvent === "function") {
        window.onUnityEvent(Pointer_stringify(str));
    }
  },

   SendMetadata: function(str) {
    if (window.onUnityMetadata && typeof window.onUnityMetadata === "function") {
        window.onUnityMetadata(Pointer_stringify(str));
    }
  },

});