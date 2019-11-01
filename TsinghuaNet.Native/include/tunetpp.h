#ifndef TUNETPP_H
#define TUNETPP_H

#include "tunet.h"
#include <chrono>
#include <exception>
#include <functional>
#include <string>
#include <vector>

#ifdef USE_INET_ADDR
#ifdef _WIN32
#include <WinSock2.h>
#else
#include <arpa/inet.h>
#endif // _WIN32
#endif // USE_INET_ADDR

namespace tunet
{
    struct tunet_exception : std::exception
    {
    private:
        std::string message;

    public:
        tunet_exception(const char* message, int32_t len) noexcept : message(message, len) {}

        const char* what() const noexcept override { return message.c_str(); }
    };

    struct flux
    {
        std::string username;
        std::int64_t bytes;
        std::chrono::seconds online_time;
        double balance;
    };

    struct user
    {
        std::int64_t address;
        std::chrono::system_clock::time_point login_time;
        std::string client;
    };

    struct detail
    {
        std::chrono::system_clock::time_point login_time;
        std::chrono::system_clock::time_point logout_time;
        std::int64_t bytes;
    };

    class helper
    {
    private:
        std::string username;
        std::string password;
        tunet_credential cred;

    public:
        helper(const std::string& username = {}, const std::string& password = {}, tunet_state state = tunet_unknown)
            : username(username), password(password)
        {
            cred = { this->username.c_str(), this->password.c_str(), state };
        }

    private:
        tunet_exception last_error() const
        {
            char message[256];
            std::int32_t len = tunet_last_err(message, sizeof(message));
            return tunet_exception(message, len);
        }

        template <typename F, typename... Args>
        std::int32_t invoke(F&& f, Args&&... args) const
        {
            std::int32_t len = f(std::forward<Args>(args)...);
            if (len < 0)
                throw last_error();
            return len;
        }

        struct invoke_guard
        {
        private:
            std::function<void()> fd;

        public:
            invoke_guard(std::function<void()>&& fd) : fd(std::move(fd)) {}
            ~invoke_guard() { fd(); }
        };

    public:
        void login() const { invoke(tunet_login, &cred); }

        void logout() const { invoke(tunet_logout, &cred); }

        flux status() const
        {
            tunet_flux f = {};
            char username[32];
            f.username = username;
            f.username_length = sizeof(username);
            std::int32_t len = invoke(tunet_status, &cred, &f);
            return { std::string(f.username, len), f.flux, std::chrono::seconds(f.online_time), f.balance };
        }

        void usereg_login() const { invoke(tunet_usereg_login, &cred); }

        void usereg_logout() const { invoke(tunet_usereg_logout, &cred); }

        void usereg_drop(std::int64_t addr) const { invoke(tunet_usereg_drop, &cred, addr); }

#ifdef USE_INET_ADDR

        void usereg_drop(const char* addr) const
        {
            usereg_drop(::inet_addr(addr));
        }

#endif // USE_INET_ADDR

        std::vector<user> usereg_users() const
        {
            invoke_guard guard([this]() { invoke(tunet_usereg_users_destory); });
            std::int32_t count = invoke(tunet_usereg_users, &cred);
            std::vector<user> result;
            tunet_user user = {};
            char client[64];
            user.client = client;
            user.client_length = sizeof(client);
            for (std::int32_t i = 0; i < count; i++)
            {
                std::int32_t len = invoke(tunet_usereg_users_fetch, i, &user);
                result.push_back({ user.address, std::chrono::system_clock::time_point(std::chrono::seconds(user.login_time)), std::string(user.client, len) });
            }
            return result;
        }

        std::vector<detail> usereg_details(tunet_detail_order order = tunet_detail_logout_time, bool descending = false) const
        {
            invoke_guard guard([this]() { invoke(tunet_usereg_details_destory); });
            std::int32_t count = invoke(tunet_usereg_details, &cred, order, descending ? 1 : 0);
            std::vector<detail> result;
            tunet_detail detail = {};
            for (std::int32_t i = 0; i < count; i++)
            {
                invoke(tunet_usereg_details_fetch, i, &detail);
                result.push_back({ std::chrono::system_clock::time_point(std::chrono::seconds(detail.login_time)), std::chrono::system_clock::time_point(std::chrono::seconds(detail.logout_time)), detail.flux });
            }
            return result;
        }
    };
} // namespace tunet

#endif // !TUNETPP_H