mergeInto(LibraryManager.library, {

  Init: function () {
    // window.alert("Init test!");
  },

  AddEvent: function(str) {
    var evtData = {
      event: Pointer_stringify(str)
    };
    
    if (!window.data || !window.data.thorEvents) {
        window.data = {
            thorEvents: []
        }
    }
    window.data.thorEvents.push(evtData);
    console.log("Event: ", evtData);
  },

});