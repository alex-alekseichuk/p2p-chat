#ifndef __EXPATCC__H__
#define __EXPATCC__H__

#include <string>
#include <iostream>

#include <expat.h>

namespace expat
{
	class Expat
	{
	public:
		Expat()
		:
		m_parser(NULL),
		m_depth(0)
		{
			m_parser = ::XML_ParserCreate(NULL);
			if (m_parser)
			{
				::XML_SetUserData(m_parser, this);
				::XML_SetElementHandler(m_parser, Expat::startElementCallback, Expat::endElementCallback);
				::XML_SetCharacterDataHandler(m_parser, Expat::charDataCallback);
			}
		}
		virtual ~Expat()
		{
			if (m_parser)
			{
				::XML_ParserFree(m_parser);
				m_parser = NULL;
			}
		}

		XML_Status parse(const char *s, int len, int isFinal);
		XML_Error getErrorCode();
		int getCurrentLineNumber();

	private:
		static void startElementCallback(void *userData, const char* name, const char** atts);
		static void endElementCallback(void *userData, const char* name);
		static void charDataCallback(void *userData, const char* s, int len);

		virtual void startElement(const char* name, const char** atts);
		virtual void endElement(const char* name);
		virtual void charData(const char *s, int len);

	protected:
		inline size_t getDepth() {return m_depth;}

	private:
		::XML_Parser m_parser;
		size_t m_depth;

	};

	inline void
	Expat::startElementCallback(void *userData, const char* name, const char** atts)
	{
		Expat* pExpat = static_cast<Expat*>(userData);
		pExpat->startElement(name, atts);
		++ pExpat->m_depth;
	}

	inline void
	Expat::endElementCallback(void *userData, const char* name)
	{
		Expat* pExpat = static_cast<Expat*>(userData);
		-- pExpat->m_depth;
		pExpat->endElement(name);
	}

	inline void
	Expat::charDataCallback(void *userData, const char* s, int len)
	{
		static_cast<Expat*>(userData)->charData(s, len);
	}


	inline XML_Status
	Expat::parse(const char *s, int len, int isFinal)
	{
//std::string ss(s, len);
//std::cout << ss << std::endl;
		return ::XML_Parse(m_parser, s, len, isFinal);
	}

	inline XML_Error
	Expat::getErrorCode()
	{
		return ::XML_GetErrorCode(m_parser);
	}

	inline int 
	Expat::getCurrentLineNumber()
	{
		return ::XML_GetCurrentLineNumber(m_parser);
	}

}

#endif
