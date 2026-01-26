namespace DaikinManagerV2.Models;

/// <summary>
/// Represents the current state of a page's data loading lifecycle.
/// </summary>
public enum PageState
{
    /// <summary>
    /// Initial state - waiting for first data fetch to complete.
    /// </summary>
    Loading,
    
    /// <summary>
    /// Connection failed or data fetch failed.
    /// </summary>
    Error,
    
    /// <summary>
    /// Connected and data has been successfully loaded.
    /// </summary>
    Ready
}
