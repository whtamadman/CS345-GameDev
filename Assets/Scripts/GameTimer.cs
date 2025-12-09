using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class GameTimer : MonoBehaviour
{
    [Header("Timer Settings")]
    [SerializeField] private bool startOnAwake = true;
    [SerializeField] private bool countUp = true; // true for count up, false for countdown
    [SerializeField] private float startTime = 0f; // Starting time in seconds (for countdown mode)
    
    [Header("UI References")]
    [SerializeField] private Text timerText; // Legacy UI Text
    [SerializeField] private TextMeshProUGUI timerTextTMP; // TextMeshPro Text
    
    [Header("Timer Control")]
    [SerializeField] private bool isPaused = false;
    [SerializeField] private bool resetOnRestart = true;
    
    // Private variables
    private float currentTime;
    private bool isRunning = false;
    
    // Events
    public System.Action OnTimerStart;
    public System.Action OnTimerStop;
    public System.Action OnTimerReset;
    public System.Action<float> OnTimerUpdate; // Passes current time in seconds
    
    void Awake()
    {
        // Auto-detect text components if not assigned
        if (timerText == null)
            timerText = GetComponent<Text>();
        if (timerTextTMP == null)
            timerTextTMP = GetComponent<TextMeshProUGUI>();
            
        // Initialize timer
        currentTime = countUp ? 0f : startTime;
        
        if (startOnAwake)
        {
            StartTimer();
        }
        
        UpdateTimerDisplay();
    }
    
    void Update()
    {
        if (isRunning && !isPaused)
        {
            if (countUp)
            {
                currentTime += Time.deltaTime;
            }
            else
            {
                currentTime -= Time.deltaTime;
                
                // Stop countdown at zero
                if (currentTime <= 0f)
                {
                    currentTime = 0f;
                    StopTimer();
                }
            }
            
            UpdateTimerDisplay();
            OnTimerUpdate?.Invoke(currentTime);
        }
    }
    
    /// <summary>
    /// Start the timer
    /// </summary>
    public void StartTimer()
    {
        isRunning = true;
        isPaused = false;
        OnTimerStart?.Invoke();
        Debug.Log("Timer started");
    }
    
    /// <summary>
    /// Stop the timer
    /// </summary>
    public void StopTimer()
    {
        isRunning = false;
        OnTimerStop?.Invoke();
        Debug.Log($"Timer stopped at: {FormatTime(currentTime)}");
    }
    
    /// <summary>
    /// Pause/Resume the timer
    /// </summary>
    public void TogglePause()
    {
        isPaused = !isPaused;
        Debug.Log($"Timer {(isPaused ? "paused" : "resumed")}");
    }
    
    /// <summary>
    /// Reset the timer
    /// </summary>
    public void ResetTimer()
    {
        currentTime = countUp ? 0f : startTime;
        UpdateTimerDisplay();
        OnTimerReset?.Invoke();
        Debug.Log("Timer reset");
    }
    
    /// <summary>
    /// Restart the timer (reset and start)
    /// </summary>
    public void RestartTimer()
    {
        if (resetOnRestart)
        {
            ResetTimer();
        }
        StartTimer();
    }
    
    /// <summary>
    /// Set the timer to a specific time
    /// </summary>
    /// <param name="timeInSeconds">Time to set in seconds</param>
    public void SetTime(float timeInSeconds)
    {
        currentTime = timeInSeconds;
        UpdateTimerDisplay();
    }
    
    /// <summary>
    /// Add time to the current timer
    /// </summary>
    /// <param name="timeToAdd">Time to add in seconds</param>
    public void AddTime(float timeToAdd)
    {
        currentTime += timeToAdd;
        if (!countUp && currentTime < 0f)
            currentTime = 0f;
        UpdateTimerDisplay();
    }
    
    /// <summary>
    /// Get the current time in seconds
    /// </summary>
    /// <returns>Current time in seconds</returns>
    public float GetCurrentTime()
    {
        return currentTime;
    }
    
    /// <summary>
    /// Get the current time formatted as string
    /// </summary>
    /// <returns>Formatted time string</returns>
    public string GetFormattedTime()
    {
        return FormatTime(currentTime);
    }
    
    /// <summary>
    /// Check if timer is running
    /// </summary>
    /// <returns>True if timer is running</returns>
    public bool IsRunning()
    {
        return isRunning && !isPaused;
    }
    
    /// <summary>
    /// Check if timer is paused
    /// </summary>
    /// <returns>True if timer is paused</returns>
    public bool IsPaused()
    {
        return isPaused;
    }
    
    /// <summary>
    /// Update the timer display text
    /// </summary>
    private void UpdateTimerDisplay()
    {
        string timeString = FormatTime(currentTime);
        
        // Update legacy UI Text
        if (timerText != null)
        {
            timerText.text = timeString;
        }
        
        // Update TextMeshPro Text
        if (timerTextTMP != null)
        {
            timerTextTMP.text = timeString;
        }
    }
    
    /// <summary>
    /// Format time as MM:SS (minutes:seconds)
    /// </summary>
    /// <param name="timeInSeconds">Time to format</param>
    /// <returns>Formatted time string</returns>
    private string FormatTime(float timeInSeconds)
    {
        // Ensure we don't show negative time
        float time = Mathf.Max(0f, timeInSeconds);
        
        int minutes = Mathf.FloorToInt(time / 60f);
        int seconds = Mathf.FloorToInt(time % 60f);
        
        return string.Format("{0:00}:{1:00}", minutes, seconds);
    }
    
    /// <summary>
    /// Context menu methods for testing in editor
    /// </summary>
    [ContextMenu("Start Timer")]
    private void ContextStartTimer()
    {
        StartTimer();
    }
    
    [ContextMenu("Stop Timer")]
    private void ContextStopTimer()
    {
        StopTimer();
    }
    
    [ContextMenu("Reset Timer")]
    private void ContextResetTimer()
    {
        ResetTimer();
    }
    
    [ContextMenu("Toggle Pause")]
    private void ContextTogglePause()
    {
        TogglePause();
    }
}