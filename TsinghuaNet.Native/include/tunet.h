#ifndef TUNET_H
#define TUNET_H

#ifdef __cplusplus
extern "C"
{
#endif // __cplusplus

#include <stdint.h>

    enum net_state
    {
        net_unknown,
        net_net,
        net_auth4,
        net_auth6
    };

    typedef struct net_credential
    {
        const char* username;
        const char* password;
        net_state state;
    } net_credential;

    typedef struct net_flux
    {
        char* username;
        int32_t username_length;
        int64_t flux;
        int64_t online_time;
        double balance;
    } net_flux;

    int32_t tunet_last_err(char* message, int32_t len);
    int32_t tunet_login(const net_credential* cred, char* message, int32_t len);
    int32_t tunet_logout(const net_credential* cred, char* message, int32_t len);
    int32_t tunet_status(const net_credential* cred, net_flux* flux);
    int32_t tunet_usereg_login(const net_credential* cred, char* message, int32_t len);
    int32_t tunet_usereg_logout(const net_credential* cred, char* message, int32_t len);
    int32_t tunet_usereg_drop(const net_credential* cred, int64_t addr, char* message, int32_t len);

#ifdef __cplusplus
}
#endif // __cplusplus

#endif // !TUNET_H
