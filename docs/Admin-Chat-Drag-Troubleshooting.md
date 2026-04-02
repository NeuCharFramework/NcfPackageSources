# Module Drag-and-Drop - Troubleshooting Guide

## ✅ Fixes Already Completed

### 1. Drag initialization timing adjustment
**Issue**: Module list loads asynchronously, so items may not be rendered at mounted time.
**Fix**: Move initializeModuleDrag() call to run after getXncfOpening() completes.

### 2. Event handling enhancement
**Issue**: dragover did not prevent default behavior, so drop could not fire.
**Fix**: Added event.preventDefault() and dropEffect in handleDragOver().

### 3. Style optimization
- Increased drop zone height to 100px.
- Added more explicit drag-and-drop hints.
- Enhanced drag visual feedback (highlight, scale, shadow).
- Added cursor: grab/grabbing styles.

### 4. Debug output
- Added console.log output for easier troubleshooting.
- Added user-friendly error messages.

---

## 🔍 Troubleshooting Steps

### Step 1: Check console logs

Open browser dev tools (F12) and check Console:

1. **Expected on page load**:
   ```
   Found module card count: X
   Module drag initialization complete, bound X modules
   ```

2. **If you see**:
   ```
   Found module card count: 0
   Module cards not found, retrying in 200ms
   ```
   - This means modules are still loading and retry will happen automatically.

3. **Expected while dragging a module**:
   ```
   Drag start module: [module name] {uid: "...", name: "..."}
   Drop zone highlighted
   ```

4. **Expected when dropping module**:
   ```
   Drop operation detected DragEvent {...}
   Received data: {"uid":"...","name":"..."}
   Parsed module data: {uid: "...", name: "..."}
   Module added successfully, current selected modules: [...]
   ```

### Step 2: Verify module draggable state

Run in Console:

```javascript
// Check module count
document.querySelectorAll('#xncf-modules-area .xncf-item').length

// Check draggable attribute count
document.querySelectorAll('#xncf-modules-area .xncf-item[draggable="true"]').length

// Check drop zone element
document.querySelector('.chat-module-drop-zone')
```

**Expected**:
- First line returns module count (for example, 8 or more).
- Second line returns the same count.
- Third line returns an HTMLDivElement.

### Step 3: Manual drag-and-drop test

1. Open page: http://localhost:5000/Admin
2. Wait for full load: ensure module cards are visible.
3. Try drag:
   - Hover over any module card.
   - Cursor should become grab.
   - Hold left mouse button to start dragging.
   - Module card should become semi-transparent.
   - Drop zone should highlight.
4. Release mouse:
   - Release inside drop zone.
   - You should see message: Module added: [module name].
   - Drop zone displays selected module tags.

---

## 🐛 Common Problems and Solutions

### Problem 1: Module cards cannot be dragged (cursor does not change)

**Possible causes**:
- Drag initialization did not run.
- Module list has not finished loading.

**Solutions**:
1. Refresh page (Ctrl+F5 hard refresh).
2. Execute manually in Console:
   ```javascript
   app.initializeModuleDrag()
   ```
3. Check Console for errors.

### Problem 2: Drag works, but drop does not

**Possible causes**:
- Drop zone is not correctly bound to drop event.
- dragover does not prevent default behavior.

**Solutions**:
1. Check drop zone exists:
   ```javascript
   console.log(document.querySelector('.chat-module-drop-zone'))
   ```
2. Check Vue instance methods:
   ```javascript
   console.log(typeof app.handleModuleDrop)
   console.log(typeof app.handleDragOver)
   ```
3. Verify bindings in Index.cshtml:
   ```html
   @@drop.prevent="handleModuleDrop"
   @@dragover.prevent="handleDragOver"
   ```

### Problem 3: Drop zone does not highlight while dragging

**Possible causes**:
- CSS selector is incorrect.
- Class name was not added successfully.

**Solutions**:
1. Check element class list while dragging:
   ```javascript
   document.querySelector('.chat-module-drop-zone').classList
   ```
   Should include highlight class.

2. Check style loaded:
   ```javascript
   getComputedStyle(document.querySelector('.chat-module-drop-zone')).background
   ```

### Problem 4: Console shows Module data not found

**Possible causes**:
- dataTransfer payload failed.
- Browser security restrictions.

**Solutions**:
1. Check browser version (latest Chrome/Edge/Firefox recommended).
2. Check extension interference (try Incognito mode).
3. Manually test dataTransfer:
   ```javascript
   // Add breakpoint in dragstart and verify
   event.dataTransfer.setData('text/plain', 'test')
   ```

---

## 🔧 Temporary Workaround

If drag-and-drop still fails, use click-to-select as fallback:

### Alternative: click to select module

Add before initializeModuleDrag() in Index.js:

```javascript
initializeModuleClick() {
  this.$nextTick(() => {
    const moduleCards = document.querySelectorAll('#xncf-modules-area .xncf-item');

    moduleCards.forEach((card) => {
      const clickHandler = (event) => {
        // If clicking links/buttons, skip selection logic
        if (event.target.tagName === 'A' || event.target.closest('a') ||
            event.target.tagName === 'BUTTON' || event.target.closest('button')) {
          return;
        }

        event.preventDefault();
        event.stopPropagation();

        const linkElement = card.querySelector('a[href*="uid="]');
        const headerElement = card.querySelector('.el-card__header span:first-child');
        const iconElement = card.querySelector('.icon');
        const versionElement = card.querySelector('.version');

        const moduleData = {
          uid: linkElement?.href?.match(/uid=([^&]+)/)?.[1] || '',
          name: headerElement?.textContent?.trim() || 'Unknown Module',
          icon: iconElement?.className || 'fa fa-cube',
          version: versionElement?.textContent?.trim() || ''
        };

        const exists = this.selectedModules.some(m => m.uid === moduleData.uid);
        if (!exists) {
          this.selectedModules.push(moduleData);
          this.$message.success(`Module added: ${moduleData.name}`);
        } else {
          this.$message.info('Module already added');
        }
      };

      // Double click to select module
      card.addEventListener('dblclick', clickHandler);
    });
  });
}
```

Then call both in getXncfOpening():

```javascript
this.initializeModuleDrag();
this.initializeModuleClick(); // Add click selection as fallback
```

---

## 📞 Need More Help?

If the issue still cannot be resolved, provide:

1. Full browser console output.
2. Browser type and version.
3. Output of the following commands:
   ```javascript
   console.log('Module count:', document.querySelectorAll('#xncf-modules-area .xncf-item').length);
   console.log('Draggable modules:', document.querySelectorAll('#xncf-modules-area .xncf-item[draggable]').length);
   console.log('Drop zone:', document.querySelector('.chat-module-drop-zone'));
   console.log('Vue instance:', app);
   ```

---

**Document Created**: 2026-03-25
**Last Updated**: 2026-03-25
**Version**: v1.0
