using System;

namespace ST.Utils.Licence
{
  public static class DateTimeHelpers
  {
    #region GetUnixEpochStartDate
    /// <summary>
    /// Метод возвращает начало эпохи в Unix
    /// </summary>
    public static DateTime GetUnixEpochStartDate()
    {
      return new DateTime( 1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc );
    }
    #endregion

    #region UnixTimeStampToDateTime
    /// <summary>
    /// Метод для преобразования времени из Unix во время, принятое в .Net
    /// </summary>
    /// <param name="unixTimeStamp">время Unix</param>
    /// <returns>время .Net</returns>
    public static DateTime UnixTimeStampToDateTime( long unixTimeStamp )
    {
      // Unix timestamp is seconds past epoch
      var dtDateTime = GetUnixEpochStartDate();

      dtDateTime = dtDateTime.AddMilliseconds( unixTimeStamp );

      return dtDateTime;
    }
    #endregion
  }
}
