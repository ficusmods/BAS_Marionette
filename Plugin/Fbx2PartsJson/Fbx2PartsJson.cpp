// Fbx2PartsJson.cpp : This file contains the 'main' function. Program execution begins and ends there.
//

#include <Windows.h>
#include <iostream>
#include <fstream>

#include <vector>

#include "ofbx.h"
#include "json.hpp"

ofbx::IScene* g_scene = nullptr;


void OpenFBXFile(char* fpath)
{
	FILE* fp;
	fopen_s(&fp, fpath, "rb");
	if (!fp) return;

	fseek(fp, 0, SEEK_END);
	long file_size = ftell(fp);
	fseek(fp, 0, SEEK_SET);
	auto* content = new ofbx::u8[file_size];
	fread(content, 1, file_size, fp);
	g_scene = ofbx::load((ofbx::u8*)content, file_size, (ofbx::u64)ofbx::LoadFlags::TRIANGULATE);
	if (!g_scene) {
		std::cout << ofbx::getError();
	}

	delete[] content;
	fclose(fp);
}

struct BoneInfo {
	std::string name;
	bool hasWeights = false;
};

struct MeshPart {
	std::string name;
	std::vector<BoneInfo> bones;
};

bool isDigit(char c)
{
	return c >= '0' && c <= '9';
}


// Returns true if the ofbx::Object name ends in '_LOD<0-9>'
bool isLODPart(ofbx::Object* obj)
{
	char* origin = obj->name;
	char* c = obj->name;
	while (*c != '\0')
	{
		c++;
	}
	if (c - origin >= 6) // Name expected to look something like: x_LODn'\0'
	{
		// See if name ends with _LOD<n> (Only a single digit is supported)
		bool flag = true;
		flag &= isDigit(*(c-1));
		flag &= *(c - 2) == 'D';
		flag &= *(c - 3) == 'O';
		flag &= *(c - 4) == 'L';
		flag &= *(c - 5) == '_';
		return flag;
	}

	std::cout << "Not a LOD part" << std::endl;
	return false;
}

bool isValidLODPart(ofbx::Object* obj)
{
	if (obj->getType() != ofbx::Object::Type::MESH)
	{
		std::cout << "LOD part root isn't a mesh!" << std::endl;
		return false;
	}

	ofbx::Object* geometry = obj->resolveObjectLink(0);
	if (geometry->getType() != ofbx::Object::Type::GEOMETRY)
	{
		std::cout << "LOD part mesh child isn't a geometry!" << std::endl;
		return false;
	}

	ofbx::Object* skin = geometry->resolveObjectLink(0);
	if (skin->getType() != ofbx::Object::Type::SKIN)
	{ 
		std::cout << "LOD part geometry child isn't a skin!" << std::endl;
		return false;
	}

	return true;
}

ofbx::Skin* getSkin(ofbx::Object* obj)
{
	ofbx::Object* geometry = obj->resolveObjectLink(0);
	ofbx::Object* skin = geometry->resolveObjectLink(0);
	return dynamic_cast<ofbx::Skin*>(skin);
}

std::vector<BoneInfo> GetMeshBones(ofbx::Skin* skin)
{
	std::vector<BoneInfo> ret{};

	for (int i = 0; i < skin->getClusterCount(); i++)
	{
		const ofbx::Cluster* cluster = skin->getCluster(i);
		BoneInfo currBoneInfo;
		currBoneInfo.hasWeights = (cluster->getWeightsCount() != 0);
		currBoneInfo.name = cluster->name;
		ret.push_back(currBoneInfo);
	}
	return ret;
}

std::vector<MeshPart> GetMeshParts()
{

	std::vector<MeshPart> ret{};

	const ofbx::Object* root = g_scene->getRoot();

	int i = 0;
	while (ofbx::Object* child = root->resolveObjectLink(i))
	{
		i++;
		std::cout << "Checking part: " << child->name << std::endl;
		if (isLODPart(child) && isValidLODPart(child))
		{
			std::cout << "Valid LOD part found: " << child->name << std::endl;
			MeshPart curr;
			curr.name = child->name;
			curr.bones = GetMeshBones(getSkin(child));
			ret.push_back(curr);
		}
	}

	return ret;
}

nlohmann::json::array_t BonesToJson(std::vector<BoneInfo> bones)
{
	using json = nlohmann::json;
	json::array_t j;

	for (auto bone : bones)
	{
		j.push_back({
			{"name", bone.name},
			{"hasWeights", bone.hasWeights}
			});
	}

	return j;
}

void DumpMeshPartsToJson(std::string fpath, const std::vector<MeshPart>& meshParts)
{
	using json = nlohmann::json;
	json j;

	for (auto part : meshParts)
	{
		j.push_back({
			{"name", part.name},
			{"bones", BonesToJson(part.bones)}
		});
	}

	std::ofstream file(fpath);
	file << j;
}

int main()
{
	LPWSTR* szArgList;
	int argCount;
	char filepath[2048];
	szArgList = CommandLineToArgvW(GetCommandLineW(), &argCount);
	if (argCount < 1)
	{
		std::cout << "Provide an FBX file as argument" << std::endl;
		return 1;
	}

	size_t pReturnValue;
	wcstombs_s(&pReturnValue, filepath, (size_t)2048,
		szArgList[1], (size_t)2048 - 1);
	
	OpenFBXFile(filepath);

	std::vector<MeshPart> meshParts = GetMeshParts();
	DumpMeshPartsToJson(std::string(filepath) + ".parts", meshParts);

	LocalFree(szArgList);
	
	return 0;
}
