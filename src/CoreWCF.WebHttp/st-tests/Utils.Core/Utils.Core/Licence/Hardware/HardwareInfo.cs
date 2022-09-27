#if NET6_0_OR_GREATER
#else

using System.Management;
using System.Text;

namespace ST.Utils.Licence
{
  public class HardwareInfo
  {
    #region GetHardwareInfo
    /// <summary>
    /// Метод производит сбор информации об аппартном обеспечинии, 
    /// на котором выполняется текущая сборка и возвращает массив байт
    /// описывающий аппартное обеспечиние.
    /// </summary>
    /// <returns>массив байт описывающий аппартное обеспечиние.</returns>
    public static byte[] GetHardwareInfo()
    {
      var collectedInfo = string.Empty;

      var searcher = new ManagementObjectSearcher("select * from Win32_BaseBoard");

      foreach (ManagementObject share in searcher.Get())
        collectedInfo += share.GetPropertyValue("SerialNumber").ToString();

      searcher.Query = new ObjectQuery("select * from Win32_Processor");

      foreach (ManagementObject share in searcher.Get())
        collectedInfo += share.GetPropertyValue("ProcessorID").ToString();

      var disk = new ManagementObject(@"Win32_LogicalDisk.DeviceId=""c:""");

      disk.Get();

      collectedInfo += disk["VolumeSerialNumber"].ToString();

      return Encoding.ASCII.GetBytes(collectedInfo);
    }
    #endregion
  }
}

#endif