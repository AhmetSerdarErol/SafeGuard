namespace SafeGuard.Mobile.Services
{
    public class LocationService
    {
        public async Task<Location?> GetCurrentLocationAsync()
        {
            try
            {
                // 1. Önce cihazın hafızasındaki son bilinen konuma bak (Çok Hızlıdır)
                Location? location = await Geolocation.Default.GetLastKnownLocationAsync();

                // 2. Eğer hafızada yoksa, uydudan taze konum iste (Biraz daha yavaştır ama kesindir)
                if (location == null)
                {
                    // Yüksek doğrulukta konum iste, en fazla 10 saniye bekle
                    var request = new GeolocationRequest(GeolocationAccuracy.High, TimeSpan.FromSeconds(15));
                    location = await Geolocation.Default.GetLocationAsync(request);
                }

                return location;
            }
            catch (FeatureNotSupportedException)
            {
                // Cihazda GPS yoksa
                System.Diagnostics.Debug.WriteLine("GPS Desteklenmiyor.");
                return null;
            }
            catch (FeatureNotEnabledException)
            {
                // Kullanıcı GPS'i kapattıysa
                System.Diagnostics.Debug.WriteLine("GPS Kapalı.");
                return null;
            }
            catch (PermissionException)
            {
                // İzin verilmediyse
                System.Diagnostics.Debug.WriteLine("Konum İzni Yok.");
                return null;
            }
            catch (Exception ex)
            {
                // Başka bir hata
                System.Diagnostics.Debug.WriteLine($"Konum Hatası: {ex.Message}");
                return null;
            }
        }
    }
}