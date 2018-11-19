#include "pch.h"

#include "LanHelper.h"
#include <map>
#include <string>
#include <winrt/Windows.Data.Json.h>
#include <winrt/Windows.Networking.Connectivity.h>

using namespace std;
using namespace winrt;
using namespace Windows::Foundation;
using namespace Windows::Data::Json;
using namespace Windows::Networking::Connectivity;
using namespace Windows::Storage;

namespace winrt::TsinghuaNetUWP
{
    tuple<InternetStatus, hstring> GetCurrentInternetStatus()
    {
        auto profile = NetworkInformation::GetInternetConnectionProfile();
        if (profile == nullptr)
            return make_tuple(InternetStatus::Unknown, hstring());
        auto cl = profile.GetNetworkConnectivityLevel();
        if (cl == NetworkConnectivityLevel::None)
            return make_tuple(InternetStatus::None, hstring());
        if (profile.IsWwanConnectionProfile())
        {
            return make_tuple(InternetStatus::Wwan, hstring());
        }
        else if (profile.IsWlanConnectionProfile())
        {
            auto details = profile.WlanConnectionProfileDetails();
            return make_tuple(InternetStatus::Wlan, details.GetConnectedSsid());
        }
        else
        {
            return make_tuple(InternetStatus::Lan, hstring());
        }
    }

    NetState LanState;
    NetState WwanState;
    map<wstring, NetState> SsidStateMap;

    IAsyncAction LoadStates()
    {
        StorageFolder folder = ApplicationData::Current().RoamingFolder();
        bool create = false;
        StorageFile file(nullptr);
        try
        {
            file = co_await folder.GetFileAsync(L"states.json");
            create = false;
        }
        catch (hresult_error const&)
        {
            create = true;
        }
        hstring json;
        if (file)
            json = co_await FileIO::ReadTextAsync(file);
        else
            json = co_await FileIO::ReadTextAsync(co_await StorageFile::StorageFile::GetFileFromApplicationUriAsync(Uri(L"ms-appx:///states.json")));
        auto obj = JsonObject::Parse(json);
        LanState = (NetState)(int32_t)(obj.GetNamedNumber(L"lan"));
        WwanState = (NetState)(int32_t)(obj.GetNamedNumber(L"wwan"));
        auto arr = obj.GetNamedObject(L"wlan");
        for (auto pair : arr)
        {
            SsidStateMap.emplace(wstring(pair.Key()), (NetState)(int32_t)(pair.Value().GetNumber()));
        }
        if (create)
        {
            file = co_await folder.CreateFileAsync(L"states.json");
            co_await FileIO::WriteTextAsync(file, json);
        }
    }

    NetState GetLanState()
    {
        return LanState;
    }

    NetState GetWwanState()
    {
        return WwanState;
    }

    NetState GetWlanState(hstring const& ssid)
    {
        auto it = SsidStateMap.find(wstring(ssid));
        if (it == SsidStateMap.end())
            return NetState::Unknown;
        return it->second;
    }
} // namespace winrt::TsinghuaNetUWP
