using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;

public static class ProfileFileManager
{
    private static readonly SemaphoreSlim semaphore = new SemaphoreSlim(1);
    private static readonly string profilesFileName = "profiles.txt";

    private static string GetProfilesFilePath()
    {
        string appDataPath = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
        string profilesDirectoryPath = Path.Combine(appDataPath, "VkLogs", "profiles");
        string profilesFilePath = Path.Combine(profilesDirectoryPath, profilesFileName);

        try
        {
            if (!Directory.Exists(profilesDirectoryPath))
            {
                Directory.CreateDirectory(profilesDirectoryPath);
            }

            if (!File.Exists(profilesFilePath))
            {
                using (File.Create(profilesFilePath)) { }
            }
        }
        catch (IOException ex)
        {
            Console.WriteLine($"Ошибка при создании файла профилей: {ex.Message}");
        }

        return profilesFilePath;
    }

    public static void AddProfile(string profileId, string applicationName)
    {
        try
        {
            semaphore.Wait();

            var profiles = File.ReadAllLines(GetProfilesFilePath()).ToList();

            var existingProfileIndex = profiles.FindIndex(p => p.StartsWith(profileId));
            if (existingProfileIndex == -1)
            {
                profiles.Add($"{profileId}|{applicationName}");
                File.WriteAllLines(GetProfilesFilePath(), profiles);
            }
            else
            {
                var existingProfileApplicationName = profiles[existingProfileIndex].Split("|")[1];
                if (existingProfileApplicationName != applicationName)
                {
                    profiles[existingProfileIndex] = $"{profileId}|{existingProfileApplicationName},{applicationName}";
                    File.WriteAllLines(GetProfilesFilePath(), profiles);
                }
            }
        }
        catch (IOException ex)
        {
            Console.WriteLine($"Ошибка при добавлении профиля: {ex.Message}");
        }
        finally
        {
            semaphore.Release();
        }
    }

    public static bool IsProfileUsed(string profileId, string applicationName)
    {
        try
        {
            semaphore.Wait();

            var profiles = File.ReadAllLines(GetProfilesFilePath()).ToList();

            var existingProfileIndex = profiles.FindIndex(p => p.StartsWith(profileId));
            if (existingProfileIndex == -1)
            {
                return false;
            }

            var existingProfileApplicationNames = profiles[existingProfileIndex].Split("|")[1].Split(",");
            return existingProfileApplicationNames.Contains(applicationName);
        }
        catch (IOException ex)
        {
            Console.WriteLine($"Ошибка при проверке использования профиля: {ex.Message}");
            return false;
        }
        finally
        {
            semaphore.Release();
        }
    }

    public static List<string> GetUsedProfiles(string applicationName)
    {
        try
        {
            semaphore.Wait();

            var profiles = File.ReadAllLines(GetProfilesFilePath()).ToList();

            var usedProfiles = profiles
                .Where(p => p.Split("|")[1].Split(",").Contains(applicationName))
                .Select(p => p.Split("|")[0])
                .ToList();

            return usedProfiles;
        }
        catch (IOException ex)
        {
            Console.WriteLine($"Ошибка при получении списка используемых профилей: {ex.Message}");
            return new List<string>();
        }
        finally
        {
            semaphore.Release();
        }
    }

    public static void RemoveProfile(string profileId, string applicationName)
    {
        try
        {
            semaphore.Wait();

            var profiles = File.ReadAllLines(GetProfilesFilePath()).ToList();

            var existingProfileIndex = profiles.FindIndex(p => p.StartsWith(profileId));
            if (existingProfileIndex != -1)
            {
                var existingProfileApplicationNames = profiles[existingProfileIndex].Split("|")[1].Split(",");
                var updatedProfileApplicationNames = existingProfileApplicationNames.Where(n => n != applicationName).ToList();
                if (updatedProfileApplicationNames.Count > 0)
                {
                    var updatedProfileData = $"{profileId}|{string.Join(",", updatedProfileApplicationNames)}";
                    profiles[existingProfileIndex] = updatedProfileData;
                    File.WriteAllLines(GetProfilesFilePath(), profiles);
                }
                else
                {
                    profiles.RemoveAt(existingProfileIndex);
                    File.WriteAllLines(GetProfilesFilePath(), profiles);
                }
            }
        }
        catch (IOException ex)
        {
            Console.WriteLine($"Ошибка при удалении профиля: {ex.Message}");
        }
        finally
        {
            semaphore.Release();
        }

    }
}

