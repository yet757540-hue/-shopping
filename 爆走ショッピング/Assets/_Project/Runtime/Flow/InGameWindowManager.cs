using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

[DisallowMultipleComponent]
public class InGameWindowManager : MonoBehaviour
{
    [Serializable]
    public sealed class WindowEntry
    {
        [SerializeField] private string id = "Window";
        [SerializeField] private GameObject root;
        [SerializeField] private bool pauseGameTime = true;
        [SerializeField] private UnityEvent opened = new UnityEvent();
        [SerializeField] private UnityEvent closed = new UnityEvent();

        public string Id => string.IsNullOrWhiteSpace(id) ? "Window" : id;
        public GameObject Root => root;
        public bool PauseGameTime => pauseGameTime;
        public bool IsOpen => root != null && root.activeSelf;

        public WindowEntry()
        {
        }

        public WindowEntry(string id, GameObject root, bool pauseGameTime)
        {
            this.id = id;
            this.root = root;
            this.pauseGameTime = pauseGameTime;
        }

        public bool Matches(string windowId)
        {
            return string.Equals(Id, windowId, StringComparison.OrdinalIgnoreCase);
        }

        public bool SetVisible(bool visible, bool invokeEvents)
        {
            if (root == null)
            {
                return false;
            }

            bool changed = root.activeSelf != visible;
            root.SetActive(visible);

            if (!changed || !invokeEvents)
            {
                return changed;
            }

            if (visible)
            {
                opened.Invoke();
            }
            else
            {
                closed.Invoke();
            }

            return true;
        }
    }

    [Header("References")]
    [SerializeField] private GameTimePauseManager pauseManager;

    [Header("Windows")]
    [SerializeField] private WindowEntry[] windows = Array.Empty<WindowEntry>();
    [SerializeField] private bool hideWindowsOnAwake = true;
    [SerializeField] private bool allowMultipleOpenWindows = false;
    [SerializeField] private bool pauseUnregisteredWindows = true;

    [Header("Events")]
    [SerializeField] private UnityEvent anyWindowOpened = new UnityEvent();
    [SerializeField] private UnityEvent anyWindowClosed = new UnityEvent();

    private readonly List<WindowEntry> runtimeWindows = new List<WindowEntry>();
    private readonly string pauseSourceId = "InGameWindowManager:" + Guid.NewGuid().ToString("N");
    private WindowEntry currentEntry;
    private GameObject currentUnregisteredWindow;

    public bool HasOpenWindow => HasAnyOpenWindow();
    public string CurrentWindowId => currentEntry != null ? currentEntry.Id : string.Empty;

    public void Initialize(GameTimePauseManager configuredPauseManager)
    {
        pauseManager = configuredPauseManager;
    }

    private string PauseSourceId => pauseSourceId;

    private void Awake()
    {
        ResolveReferences();

        if (hideWindowsOnAwake)
        {
            CloseAllWindows(false);
        }

        RefreshWindowPause();
    }

    private void OnDisable()
    {
        if (pauseManager != null)
        {
            pauseManager.ReleasePause(PauseSourceId);
        }
    }

    public void ShowWindow(string id)
    {
        WindowEntry entry = FindWindow(id);

        if (entry == null)
        {
            Debug.LogWarning("[InGameWindowManager] Window is not registered: " + id, this);
            return;
        }

        ShowEntry(entry);
    }

    public void ShowWindow(GameObject windowRoot)
    {
        if (windowRoot == null)
        {
            return;
        }

        if (!allowMultipleOpenWindows)
        {
            CloseAllWindows(true);
        }

        bool changed = !windowRoot.activeSelf;
        windowRoot.SetActive(true);
        currentEntry = null;
        currentUnregisteredWindow = windowRoot;

        if (changed)
        {
            anyWindowOpened.Invoke();
        }

        RefreshWindowPause();
    }

    public void HideWindow(string id)
    {
        WindowEntry entry = FindWindow(id);

        if (entry == null)
        {
            return;
        }

        HideEntry(entry);
    }

    public void HideWindow(GameObject windowRoot)
    {
        if (windowRoot == null)
        {
            return;
        }

        bool changed = windowRoot.activeSelf;
        windowRoot.SetActive(false);

        if (currentUnregisteredWindow == windowRoot)
        {
            currentUnregisteredWindow = null;
        }

        if (changed)
        {
            anyWindowClosed.Invoke();
        }

        RefreshWindowPause();
    }

    public void ToggleWindow(string id)
    {
        WindowEntry entry = FindWindow(id);

        if (entry == null)
        {
            Debug.LogWarning("[InGameWindowManager] Window is not registered: " + id, this);
            return;
        }

        if (entry.IsOpen)
        {
            HideEntry(entry);
            return;
        }

        ShowEntry(entry);
    }

    public void CloseCurrentWindow()
    {
        if (currentEntry != null && currentEntry.IsOpen)
        {
            HideEntry(currentEntry);
            return;
        }

        if (currentUnregisteredWindow != null && currentUnregisteredWindow.activeSelf)
        {
            HideWindow(currentUnregisteredWindow);
            return;
        }

        CloseAllWindows(true);
    }

    public void CloseAllWindows()
    {
        CloseAllWindows(true);
    }

    public void RegisterWindow(string id, GameObject root, bool pauseGameTime)
    {
        if (root == null)
        {
            return;
        }

        WindowEntry existing = FindWindow(id);

        if (existing != null)
        {
            runtimeWindows.Remove(existing);
        }

        runtimeWindows.Add(new WindowEntry(id, root, pauseGameTime));
        RefreshWindowPause();
    }

    private void ShowEntry(WindowEntry entry)
    {
        if (!allowMultipleOpenWindows)
        {
            CloseAllWindowsExcept(entry, true);
        }

        bool changed = entry.SetVisible(true, true);
        currentEntry = entry;
        currentUnregisteredWindow = null;

        if (changed)
        {
            anyWindowOpened.Invoke();
        }

        RefreshWindowPause();
    }

    private void HideEntry(WindowEntry entry)
    {
        bool changed = entry.SetVisible(false, true);

        if (currentEntry == entry)
        {
            currentEntry = null;
        }

        if (changed)
        {
            anyWindowClosed.Invoke();
        }

        RefreshWindowPause();
    }

    private void CloseAllWindows(bool invokeEvents)
    {
        bool closedAny = false;

        foreach (WindowEntry entry in EnumerateWindows())
        {
            if (entry != null)
            {
                closedAny |= entry.SetVisible(false, invokeEvents);
            }
        }

        if (currentUnregisteredWindow != null)
        {
            closedAny |= currentUnregisteredWindow.activeSelf;
            currentUnregisteredWindow.SetActive(false);
            currentUnregisteredWindow = null;
        }

        currentEntry = null;

        if (closedAny && invokeEvents)
        {
            anyWindowClosed.Invoke();
        }

        RefreshWindowPause();
    }

    private void CloseAllWindowsExcept(WindowEntry openEntry, bool invokeEvents)
    {
        bool closedAny = false;

        foreach (WindowEntry entry in EnumerateWindows())
        {
            if (entry == null || entry == openEntry)
            {
                continue;
            }

            closedAny |= entry.SetVisible(false, invokeEvents);
        }

        if (currentUnregisteredWindow != null)
        {
            closedAny |= currentUnregisteredWindow.activeSelf;
            currentUnregisteredWindow.SetActive(false);
            currentUnregisteredWindow = null;
        }

        if (closedAny && invokeEvents)
        {
            anyWindowClosed.Invoke();
        }
    }

    private WindowEntry FindWindow(string id)
    {
        if (string.IsNullOrWhiteSpace(id))
        {
            return null;
        }

        foreach (WindowEntry entry in EnumerateWindows())
        {
            if (entry != null && entry.Matches(id))
            {
                return entry;
            }
        }

        return null;
    }

    private IEnumerable<WindowEntry> EnumerateWindows()
    {
        if (windows != null)
        {
            foreach (WindowEntry entry in windows)
            {
                yield return entry;
            }
        }

        foreach (WindowEntry entry in runtimeWindows)
        {
            yield return entry;
        }
    }

    private bool HasAnyOpenWindow()
    {
        foreach (WindowEntry entry in EnumerateWindows())
        {
            if (entry != null && entry.IsOpen)
            {
                return true;
            }
        }

        return currentUnregisteredWindow != null && currentUnregisteredWindow.activeSelf;
    }

    private bool HasPauseWindowOpen()
    {
        foreach (WindowEntry entry in EnumerateWindows())
        {
            if (entry != null && entry.IsOpen && entry.PauseGameTime)
            {
                return true;
            }
        }

        return pauseUnregisteredWindows && currentUnregisteredWindow != null && currentUnregisteredWindow.activeSelf;
    }

    private void RefreshWindowPause()
    {
        ResolveReferences();

        if (pauseManager == null)
        {
            return;
        }

        if (HasPauseWindowOpen())
        {
            pauseManager.RequestPause(PauseSourceId);
            return;
        }

        pauseManager.ReleasePause(PauseSourceId);
    }

    private void ResolveReferences()
    {
    }
}
