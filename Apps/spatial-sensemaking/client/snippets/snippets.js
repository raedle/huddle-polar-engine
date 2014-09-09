if (Meteor.isClient) {
  Template.snippets.initSnippetPosition = function() {
    if ($("#snippet_"+this._id).css('top') !== undefined ||
      $("#snippet_"+this._id).css('left') !== undefined) return;

    var top  = this.y;
    var left = this.x;

    if (top === undefined) top = getRandomInt(0, 600);
    if (left === undefined) left = getRandomInt(0, 600);
    
    var that = this;
    Meteor.setTimeout(function() {
      $("#snippet_"+that._id).css({
        top: top,
        left: left
      });
      markSnippetDirty(that);
    }, 1);
  };

  Template.snippets.snippets = function() {
    var thisDevice = Session.get('thisDevice');
    if (thisDevice === undefined) return [];

    var snippets = Snippets.find({ device: thisDevice.id });
    return snippets.fetch();
  };

  Template.snippets.otherDevices = function() {
    return Session.get("otherDevices") || [];
  };

  // var dragDep = new Deps.Dependency();
  var dragLastPoint;
  Template.snippets.events({
    'touchstart .snippet, mousedown .snippet': function(e) {
      e.preventDefault();
      dragLastPoint = getEventLocation(e, "client");
    },

    'touchmove .snippet, mousemove .snippet': function(e) {
      // e.preventDefault();

      if (dragLastPoint === undefined) return;

      var currentPoint = getEventLocation(e, "client");
      var deltaX = currentPoint.x - dragLastPoint.x;
      var deltaY = currentPoint.y - dragLastPoint.y;

      dragLastPoint = currentPoint;

      $("#snippet_"+this._id).css({
        top: parseInt($("#snippet_"+this._id).css('top'))   + deltaY,
        left: parseInt($("#snippet_"+this._id).css('left')) + deltaX
      });
      markSnippetDirty(this);
    },

    'touchend .snippet, mouseup .snippet': function(e) {
      // e.preventDefault();
      dragLastPoint = undefined;
    }
  });

  Template.snippets.helpers({
    'thisDeviceBorderColorCSS': function() {
      return window.thisDeviceBorderColorCSS();
    },

    'deviceColorCSS': function() {
      return 'background-color: '+window.deviceColorCSS(this);
    },

    'deviceSizePositionCSS': function() {
      return window.deviceSizePositionCSS(this);
    },
  });
}

var dirtyTimer = {};
function markSnippetDirty(snippet) {
  if (dirtyTimer[snippet._id] !== undefined) return;

  dirtyTimer[snippet._id] = Meteor.setTimeout(function() {
    dirtyTimer[snippet._id] = undefined;
    Snippets.update(
      {_id: snippet._id}, 
      {$set: {
        y: parseInt($("#snippet_"+snippet._id).css('top')),
        x: parseInt($("#snippet_"+snippet._id).css('left'))
      }}
    );
  }, 5000);
}

function getEventLocation(e, type) 
{
  if (type === undefined) type = "page";

  var pos = { x: e[type+"X"], y: e[type+"Y"] };
  if (pos.x === undefined || pos.y === undefined)
  {
    pos = { x: e.originalEvent.targetTouches[0][type+"X"], y: e.originalEvent.targetTouches[0][type+"Y"] };
  }

  return pos;
}

function getRandomInt(min, max) {
  return Math.floor(Math.random() * (max - min + 1)) + min;
}
