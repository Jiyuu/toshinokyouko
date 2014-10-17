using System;

public sealed class Manager
{
    #region singleton
    private static volatile Manager instance;
    private static object syncRoot = new Object();
    System.Timers.Timer timer = new System.Timers.Timer(TimeSpan.FromDays(1).TotalMilliseconds);
   

    public static Manager Instance
    {
        get
        {
            if (instance == null)
            {
                lock (syncRoot)
                {
                    if (instance == null)
                        instance = new Manager();
                }
            }

            return instance;
        }
    }
    #endregion

    private Manager()
    {
        timer.Elapsed += timer_Elapsed;
    }

    void timer_Elapsed(object sender, System.Timers.ElapsedEventArgs e)
    {
        updateImagesList();
    }


    private void updateImagesList()
    { 
    
    }

    public string GetImage()
    {
        return null;
    }

}