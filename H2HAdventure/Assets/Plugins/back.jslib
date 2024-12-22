mergeInto(LibraryManager.library, {

  Hello: function () {
    window.alert("Hello, world!");
  },

  BrowserGoBack: function () {
    history.back();
  },
});
