#ifndef __OPENSSL__H__
#define __OPENSSL__H__

#include <string>

namespace openssl
{
	class Openssl
	{

	public:
		static std::string sha1(const char *s);

	private:
		static const char *convertToHexString(unsigned char *md);

	};

}

#endif
