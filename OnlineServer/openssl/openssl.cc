

#include "openssl.h"

#include <openssl/evp.h>
#include <openssl/sha.h>


namespace openssl
{

	std::string Openssl::sha1(const char *s)
	{
		if (!s || !(*s))
			return std::string("");

		EVP_MD_CTX c;
        unsigned char md[SHA_DIGEST_LENGTH];

		EVP_MD_CTX_init(&c);

		EVP_Digest(s, ::strlen(s), md, NULL, EVP_sha1(), NULL);

		return std::string(convertToHexString(md));
	}

	const char *Openssl::convertToHexString(unsigned char *md)
	{
		int i;
		static char buf[80];
		for (i = 0; i < SHA_DIGEST_LENGTH; i++)
			::sprintf(&(buf[i*2]), "%02x", md[i]);
		return(buf);
	}

}

