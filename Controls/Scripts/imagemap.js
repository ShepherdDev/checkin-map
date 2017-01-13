(function ($)
{
  var ImageMap = function (el, options)
  {
    this.options = options;
    this.$el = $(el);

    /* Monitor a mouse drag event */
    function mouseMove(e)
    {
      e.preventDefault();

      var $el = $(this).data('drag-element');
      var pos = { left: e.pageX - $el.data('offsetX'), top: e.pageY - $el.data('offsetY') };
      var marginLeft = parseInt($el.css('margin-left'));
      var marginTop = parseInt($el.css('margin-top'));

      if (pos.left < -marginLeft)
        pos.left = -marginLeft;
      if (pos.top < -marginTop)
        pos.top = -marginTop;
      if (pos.left > $el.parent().width() - $el.outerWidth() - marginLeft)
        pos.left = $el.parent().width() - $el.outerWidth() - marginLeft;
      if (pos.top > $el.parent().height() - $el.outerHeight() - marginTop)
        pos.top = $el.parent().height() - $el.outerHeight() - marginTop;
      $el.css('left', pos.left + 'px');
      $el.css('top', pos.top + 'px');
    }

    /* Monitor a mouse leaving the container, cancel events. */
    function mouseLeave(e)
    {
      cancelEvents($(this));
    }

    /* Ignore clicks on action blocks. */
    function ignoreClick(e)
    {
      e.preventDefault();
    }

    /* Cancel any outstanding drag event and finalize position. */
    function cancelEvents(container)
    {
      var $el = $(container).data('drag-element');
      $(container).off('mouseleave', mouseLeave);
      $(container).off('mousemove', mouseMove);

      $el.css('left', (Math.round($el.position().left / $el.parent().width() * 1000) / 10).toString().concat('%'));
      $el.css('top', (Math.round($el.position().top / $el.parent().height() * 1000) / 10).toString().concat('%'));
    }

    /* Monitor an element for dragging. */
    function monitorDrag(element)
    {
      $(element).on('mousedown', function (e)
      {
        e.preventDefault();

        $(this).data('offsetX', e.pageX - $(this).position().left);
        $(this).data('offsetY', e.pageY - $(this).position().top);
        $(this).parent().data('drag-element', $(this));
        $(this).parent().on('mousemove', mouseMove);
        $(this).parent().on('mouseleave', mouseLeave);
        $(this).parent().on('mouseup', function (e) { e.preventDefault(); cancelEvents(this); });
      });
      $(element).on('click', ignoreClick);
    }

    /* Add a new action block to the container. */
    this.addAction = function (data)
    {
      var $block = data.url ? $('<a class="imagemap-button"></a>') : $('<span class="imagemap-button"></span>');
      var $title = $('<div class="imagemap-title"></div>').text(data.title).appendTo($block);
      var $text = $('<div class="imagemap-text"></div>').html(data.text).appendTo($block);
      var halfWidth, halfHeight;

      $block.addClass(data.class);
      this.$container.append($block);

      $block.css('left', (data.x ? data.x : '0') + '%').css('top', (data.y ? data.y : '0') + '%');
      if (data.id)
      {
        $block.data('id', data.id);
      }
      if (data.url)
      {
        $block.attr('href', data.url);
      }

      /* Set the margin so that our position is center based */
      halfWidth = $block.outerWidth() / 2;
      halfHeight = $block.outerHeight() / 2;
      $block.css('margin-left', -halfWidth + 'px');
      $block.css('margin-top', -halfHeight + 'px');

      /* Verify the object stays within the container */
      if (parseFloat($block.css('left')) - halfWidth < 0)
      {
        $block.css('left', halfWidth + 'px');
      }
      else if (parseFloat($block.css('left')) + halfWidth >= this.$container.innerWidth())
      {
        $block.css('left', (this.$container.innerWidth() - halfWidth).toString().concat('px'));
      }
      if (parseFloat($block.css('top')) - halfHeight < 0)
      {
        $block.css('top', halfHeight + 'px');
      }
      else if (parseFloat($block.css('top')) + halfHeight >= this.$container.innerHeight())
      {
        $block.css('top', (this.$container.innerHeight() - halfHeight).toString().concat('px'));
      }

      if (this.options.edit)
      {
        monitorDrag($block);
      }
    };

    /* Get the data to reconstruct this image map container later. */
    this.getActions = function ()
    {
      var actions = [];
      var _this = this;

      this.$container.find('.imagemap-button').each(function ()
      {
        var data = {};

        data.class = $(this).attr('class').replace('imagemap-button label', '').trim();
        if ($(this).data('id'))
        {
          data.id = $(this).data('id');
        }
        if ($(this).attr('href'))
        {
          data.url = $(this).attr('href');
        }
        data.title = $(this).find('.imagemap-title').html();
        data.text = $(this).find('.imagemap-text').html();
        data.x = (Math.round(parseFloat($(this).css('left')) / _this.$container.innerWidth() * 1000) / 10).toString();
        data.y = (Math.round(parseFloat($(this).css('top')) / _this.$container.innerHeight() * 1000) / 10).toString();

        actions.push(data);
      });

      return actions;
    };

    /* Replace the actions with those specified. */
    this.setActions = function (actions)
    {
      var _this = this;

      this.$container.find('.imagemap-button').remove();
      actions.forEach(function (data) { _this.addAction(data); });
    };

    /* Replace the original element with the container. */
    this.$container = $('<div class="imagemap-container"></div>').insertBefore(this.$el).append(this.$el);
    if (this.options.edit)
    {
      this.$container.addClass('editable');
    }

    if (this.options.actions)
    {
      var container = this;

      if (this.$el[0].tagName === 'IMG')
      {
        if (!this.$el[0].complete || this.$el[0].naturalWidth === 0)
        {
          this.$el.on('load', function ()
          {
            container.options.actions.forEach(function (data) { container.addAction(data); });
          });
        }
        else
        {
          /* Use a timeout to give the container a chance to settle in the DOM. */
          setTimeout(function ()
          {
            container.options.actions.forEach(function (data) { container.addAction(data); });
          }, 0);
        }
      }
      else
      {
        /* Use a timeout to give the container a chance to settle in the DOM. */
        setTimeout(function ()
        {
          container.options.actions.forEach(function (data) { container.addAction(data); });
        }, 0);
      }
    }
  };

  /* Initialize a new ImageMap onto a jQuery object. */
  $.fn.ImageMap = function (option)
  {
    var value;
    var args = Array.prototype.splice.call(arguments, 1);
    var DEFAULTS = {
      edit: false
    };

    this.each(function ()
    {
      var $this = $(this);
      var data = $this.data('ImageMap.data');
      var options = $.extend({}, DEFAULTS, $this.data(), typeof option === 'object' && option);

      if (typeof option === 'string')
      {
        if (!data)
        {
          return;
        }

        value = data[option].apply(data, args);
      }

      if (!data)
      {
        data = new ImageMap(this, options);
        $this.data('ImageMap.data', data);
      }
    });

    return typeof value === 'undefined' ? this : value;
  };
})(jQuery);
