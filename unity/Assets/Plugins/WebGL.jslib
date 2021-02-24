mergeInto(LibraryManager.library, {

  Init: function () {
    // window.alert("Init test!");
    if (window.onGameLoaded && typeof(window.onGameLoaded) === 'function') {
        window.onGameLoaded();
    }
  },
  
   SendMetadata: function(str) {
    if (window.onUnityMetadata && typeof window.onUnityMetadata === "function") {
        window.onUnityMetadata(Pointer_stringify(str));
    }
  },

});