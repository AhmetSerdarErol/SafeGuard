namespace SafeGuard.Mobile.Services
{
    public class LocationService
    {
        public async Task<Location?> GetCurrentLocationAsync()
        {
            try
            {
                Location? location = await Geolocation.Default.GetLastKnownLocationAsync();

                if (location == null)
                {
                    var request = new GeolocationRequest(GeolocationAccuracy.High, TimeSpan.FromSeconds(15));
                    location = await Geolocation.Default.GetLocationAsync(request);
                }

                return location;
            }
            catch (FeatureNotSupportedException)
            {
                System.Diagnostics.Debug.WriteLine("GPS Desteklenmiyor.");
                return null;
            }
            catch (FeatureNotEnabledException)
            {
                System.Diagnostics.Debug.WriteLine("GPS Kapalı.");
                return null;
            }
            catch (PermissionException)
            {
                System.Diagnostics.Debug.WriteLine("Konum İzni Yok.");
                return null;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Konum Hatası: {ex.Message}");
                return null;
            }
        }
    }
}