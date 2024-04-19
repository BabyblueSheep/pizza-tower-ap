// https://github.com/black-sliver/gm-apclientpp/blob/main/src/gm-apclientpp.cpp

#include "../include/gm-apclientpp.h"

#include <apclient.hpp>
#include <apuuid.hpp>
#include <nlohmann/json.hpp>

#define VERSION_TUPLE {0,4,5}

#define UUID_FILE "/settings/uuid"
#define CERT_STORE "cacert.pem"

using json = nlohmann::json;

static std::unique_ptr<APClient> ap_client;
static bool wants_deathlink;
static std::mutex mutex;

double ap_connect(const char* uri)
{
	const std::lock_guard<std::mutex> lock(mutex);
	try
	{
		std::string uuid = ap_get_uuid(UUID_FILE);
		ap_client.reset(new APClient(uuid, "Pizza Tower", uri, CERT_STORE));
		ap_client->set_socket_connected_handler([]() {
		});
		ap_client->set_socket_disconnected_handler([]() {
		});
		ap_client->set_slot_connected_handler([](const json& data) {
			if (data.contains("options")) {
				(data.at("options").contains("death_link")) ? (data.at("options").at("death_link").get_to(wants_deathlink)) : wants_deathlink = false;
			}
		});
	}
	catch (...)
	{
		return 0;
	}
	return 1;
}

double ap_disconnect()
{
	const std::lock_guard<std::mutex> lock(mutex);
	try
	{
		ap_client.reset(nullptr);
	}
	catch (...)
	{
		return 0;
	}
	return 1;
}

double ap_connect_slot(const char* name, const char* password, double deathlink)
{
	const std::lock_guard<std::mutex> lock(mutex);
	try
	{
		std::list<std::string> tags = { "AP" };
		if (deathlink == 1)
			tags.push_back("DeathLink");
		ap_client->ConnectSlot(name, password, 0b101, tags, VERSION_TUPLE);
	}
	catch (...)
	{
		return 0;
	}
	return 1;
}

double ap_poll()
{
	const std::lock_guard<std::mutex> lock(mutex);
	try
	{
		ap_client->poll();
	}
	catch (...)
	{
		return 0;
	}
	return 1;
}

double ap_wants_deathlink()
{
	return wants_deathlink ? 1 : 0;
}

double ap_get_state()
{
	const std::lock_guard<std::mutex> lock(mutex);
	if (!ap_client)
		return 0.;
	return (double)ap_client->get_state();
}