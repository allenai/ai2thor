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

  GetJsonBufferLength: function() {
    return window._unityJsonBufferLength || 0;
  },

  FreeJsonBuffer: function(ptr) {
    if (typeof window.gameInstance !== "undefined") {
      window.gameInstance.Module._free(ptr);
      console.log("Freed pointer");
    }
    else {
      console.log("window.gameInstance is undefined");
    }
  },

  ObjaverseDownloadProgress: function(progress) {
     if (window.objaverseProgressCallback && typeof window.objaverseProgressCallback === "function") {
        window.objaverseProgressCallback(progress);
    }
  }

});