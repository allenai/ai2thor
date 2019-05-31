mergeInto(LibraryManager.library, {

  Init: function () {
    // window.alert("Init test!");
    if (window.game_init && typeof(window.game_init) === 'function') {
        window.game_init();
    }
  },
  
  SendEvent: function(str) {
    if (window.onUnityEvent && typeof window.onUnityEvent === "function") {
        window.onUnityEvent(Pointer_stringify(str));
    }
  },

});