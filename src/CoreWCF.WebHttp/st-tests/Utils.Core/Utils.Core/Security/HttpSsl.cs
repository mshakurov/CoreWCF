using System.Security.Cryptography.X509Certificates;


namespace ST.Utils.Security
{
  /// <summary>
  /// Вспомогательный класс для конфигурации SSL-сертификатов в HTTP.
  /// </summary>
  public static class HttpSsl
  {
    #region .Constants
    private const string CERTIFICATE_DISTINGUISHED_NAME = "CN=localhost;OU=SpaceTeamLab";
    #endregion

    #region BindToPort
    /// <summary>
    /// Устанавливает настройки порта для работы по протоколу HTTPS.
    /// </summary>
    /// <param name="port">Номер SSL порта.</param>
    /// <param name="certificate">Cертификат.</param>
    /// <param name="storeName">Название хранилища сертификатов.</param>
    public static void BindToPort(ushort port, X509Certificate2 certificate, StoreName storeName = StoreName.Root)
    {
      SetPortSettings(false, storeName, port, certificate);
    }
    #endregion

    #region CreateCertificate
    /// <summary>
    /// Создает бинарное представление сертификата.
    /// </summary>
    /// <returns>Бинарное представление сертификата.</returns>
    public static byte[] CreateCertificate()
    {
      //TODO: CreateCertificate
      throw new NotImplementedException();
    }
    #endregion

    #region FindCertificate
    /// <summary>
    /// Поиск сертификата в хранилище сертификатов.
    /// </summary>
    /// <param name="findValue">Значение, по которому производится поиск сертификата.</param>
    /// <param name="storeName">Название хранилища сертификатов.</param>
    /// <param name="findType">Тип значения, по которому производится поиск сертификата.</param>
    /// <returns>Сертификат.</returns>
    public static X509Certificate2 FindCertificate(string findValue, StoreName storeName = StoreName.Root, X509FindType findType = X509FindType.FindByThumbprint)
    {
      var store = new X509Store(storeName, StoreLocation.LocalMachine);

      try
      {
        store.Open(OpenFlags.ReadOnly);

        // We find valid and invalid certificates here!
        var col = store.Certificates.Find(findType, findValue, false);

        return col.Count > 0 ? col[0] : null;
      }
      catch (Exception exc)
      {
        throw new Exception(SR.GetString(RI.HttpsOpenStoreError), exc);
      }
      finally
      {
        store.Close();
      }
    }
    #endregion

    #region HttpInitialize
    private static void HttpInitialize()
    {
      //TODO: HttpInitialize
      throw new NotImplementedException();
    }
    #endregion

    #region InstallAndBindToPort
    /// <summary>
    /// Устанавливает сертификат и настройки порта для работы по протоколу HTTPS.
    /// </summary>
    /// <param name="port">Номер SSL порта.</param>
    /// <param name="certificate">Cертификат.</param>
    /// <param name="storeName">Название хранилища сертификатов.</param>
    public static void InstallAndBindToPort(ushort port, X509Certificate2 certificate, StoreName storeName = StoreName.Root)
    {
      InstallCertificate(certificate, storeName);

      BindToPort(port, certificate, storeName);
    }
    #endregion

    #region InstallCertificate
    /// <summary>
    /// Устанавливает сертификат.
    /// </summary>
    /// <param name="certificate">X.509 certificate.</param>
    /// <param name="storeName">Название хранилища сертификатов.</param>
    /// <returns>Отпечаток сертификата.</returns>
    public static string InstallCertificate(X509Certificate2 certificate, StoreName storeName = StoreName.Root)
    {
      UninstallCertificate(certificate.Thumbprint, storeName);

      UpdateCertificate(false, storeName, certificate);

      return certificate.Thumbprint;
    }

    /// <summary>
    /// Устанавливает сертификат.
    /// </summary>
    /// <param name="storeName">Название хранилища сертификатов.</param>
    /// <param name="certificatePath">Путь к файлу сертификата. Обрабатываются файлы только с расширением pfx. При передаче значения null сертификат создается программно.</param>
    /// <param name="certificatePassword">Пароль файла сертификата. Передача null в качестве аргумента предполагает отсутствие пароля.</param>
    /// <returns>Отпечаток сертификата.</returns>
    public static string InstallCertificate(StoreName storeName = StoreName.Root, string certificatePath = null, string certificatePassword = null)
    {
      if (certificatePath != null)
        if (certificatePath == string.Empty || Path.GetExtension(certificatePath).ToUpper() != ".PFX")
          throw new Exception(SR.GetString(RI.HttpsNoFileCertificate));

      string thumbprint = null;
      X509Certificate2 certificate = null;

      try
      {
        certificate = certificatePath != null ? LoadCertificate(certificatePath, certificatePassword, null) : LoadCertificate(null, null, CreateCertificate());

        UninstallCertificate(certificate.Thumbprint, storeName);

        UpdateCertificate(false, storeName, certificate);

        thumbprint = certificate.Thumbprint;
      }
      catch
      {
        throw;
      }

      return thumbprint;
    }
    #endregion

    #region LoadCertificate
    private static X509Certificate2 LoadCertificate(string pfxCertificatePath, string password, byte[] pfxData)
    {
      X509Certificate2 certificate = null;

      try
      {
        certificate = pfxData == null ? new X509Certificate2(pfxCertificatePath, password == null ? string.Empty : password, X509KeyStorageFlags.MachineKeySet | X509KeyStorageFlags.PersistKeySet) : new X509Certificate2(pfxData, string.Empty, X509KeyStorageFlags.MachineKeySet | X509KeyStorageFlags.PersistKeySet);
      }
      catch
      {
        throw new Exception(SR.GetString(pfxData == null ? RI.HttpsLoadCertificateError : RI.HttpsLoadCertificateFileError, pfxCertificatePath));
      }

      return certificate;
    }
    #endregion

    #region SetPortSettings
    private static void SetPortSettings(bool isRemoving, StoreName storeName, ushort port, X509Certificate2 certificate)
    {
      try
      {
        HttpInitialize();

        byte[] hash = null;

        if (!isRemoving)
          hash = certificate.GetCertHash();

        UpdateSslBinding(isRemoving, storeName, port, hash);

        UpdateUrlReservation(isRemoving, port);

        //TODO: HttpTerminate
        throw new NotImplementedException();
      }
      catch
      {
        throw;
      }
    }
    #endregion

    #region UnbindFromPort
    /// <summary>
    /// Удаляет настройки порта для работы по протоколу HTTPS.
    /// </summary>
    /// <param name="port">Номер SSL порта.</param>
    /// <param name="storeName">Название хранилища сертификатов.</param>
    public static void UnbindFromPort(ushort port, StoreName storeName = StoreName.Root)
    {
      SetPortSettings(true, storeName, port, null);
    }
    #endregion

    #region UninstallAndUnbindFromPort
    /// <summary>
    /// Удаляет сертификат и настройки порта для работы по протоколу HTTPS.
    /// </summary>
    /// <param name="port">Номер SSL порта.</param>
    /// <param name="thumbprint">Отпечаток сертификата.</param>
    /// <param name="storeName">Название хранилища сертификатов.</param>
    public static void UninstallAndUnbindFromPort(ushort port, string thumbprint, StoreName storeName = StoreName.Root)
    {
      UnbindFromPort(port, storeName);

      UninstallCertificate(thumbprint, storeName);
    }
    #endregion

    #region UninstallCertificate
    /// <summary>
    /// Удаляет сертификат.
    /// </summary>
    /// <param name="thumbprint">Отпечаток сертификата.</param>
    /// <param name="storeName">Название хранилища сертификатов.</param>
    public static void UninstallCertificate(string thumbprint, StoreName storeName = StoreName.Root)
    {
      Exec.Try(() => FindCertificate(thumbprint).IfNotNull(crt => UpdateCertificate(true, storeName, crt)), true);
    }
    #endregion

    #region UpdateCertificate
    private static void UpdateCertificate(bool isRemoving, StoreName storeName, X509Certificate2 certificate)
    {
      var store = new X509Store(storeName, StoreLocation.LocalMachine);

      try
      {
        store.Open(OpenFlags.ReadWrite);

        if (!isRemoving)
          store.Add(certificate);
        else
          store.Remove(certificate);
      }
      catch (Exception exc)
      {
        throw new Exception(SR.GetString(isRemoving ? RI.HttpsRemoveCertificateError : RI.HttpsAddCertificateError), exc);
      }
      finally
      {
        store.Close();
      }
    }
    #endregion

    #region UpdateSslBinding
    private static void UpdateSslBinding(bool isRemoving, StoreName storeName, ushort port, byte[] hash)
    {
      //TODO: UpdateSslBinding
      throw new NotImplementedException();
    }
    #endregion

    #region UpdateUrlReservation
    private static void UpdateUrlReservation(bool isRemoving, ushort port)
    {
      //TODO: UpdateSslBinding
      throw new NotImplementedException();
    }
    #endregion
  }
}
