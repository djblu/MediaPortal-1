
/* 
 *	Copyright (C) 2005 Team MediaPortal
 *	http://www.team-mediaportal.com
 *
 *  This Program is free software; you can redistribute it and/or modify
 *  it under the terms of the GNU General Public License as published by
 *  the Free Software Foundation; either version 2, or (at your option)
 *  any later version.
 *   
 *  This Program is distributed in the hope that it will be useful,
 *  but WITHOUT ANY WARRANTY; without even the implied warranty of
 *  MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the
 *  GNU General Public License for more details.
 *   
 *  You should have received a copy of the GNU General Public License
 *  along with GNU Make; see the file COPYING.  If not, write to
 *  the Free Software Foundation, 675 Mass Ave, Cambridge, MA 02139, USA. 
 *  http://www.gnu.org/copyleft/gpl.html
 *
 */
#pragma once
#include "multifilereader.h"
#include "pcrdecoder.h"
#include "demultiplexer.h"
#include "TsDuration.h"
#include "TSThread.h"
#include "rtspclient.h"
#include "memorybuffer.h"
#include <map>
using namespace std;

class CSubtitlePin;
class CAudioPin;
class CVideoPin;
class CTsReader;
class CTsReaderFilter;

DEFINE_GUID(CLSID_TSReader, 0xb9559486, 0xe1bb, 0x45d3, 0xa2, 0xa2, 0x9a, 0x7a, 0xfe, 0x49, 0xb2, 0x3f);


class CTsReaderFilter : public CSource,public TSThread,public IFileSourceFilter, public IAMFilterMiscFlags, public IAMStreamSelect
{
public:
		DECLARE_IUNKNOWN
		static CUnknown * WINAPI CreateInstance(LPUNKNOWN punk, HRESULT *phr);

private:
		CTsReaderFilter(IUnknown *pUnk, HRESULT *phr);
		~CTsReaderFilter();
		STDMETHODIMP NonDelegatingQueryInterface(REFIID riid, void ** ppv);

    // Pin enumeration
    CBasePin * GetPin(int n);
    int GetPinCount();

    // Open and close the file as necessary
public:
    STDMETHODIMP Run(REFERENCE_TIME tStart);
    STDMETHODIMP Pause();
    STDMETHODIMP Stop();
private:
	// IAMFilterMiscFlags
		virtual ULONG STDMETHODCALLTYPE		GetMiscFlags();

    //IAMStreamSelect
    STDMETHODIMP Count(DWORD* streamCount);
    STDMETHODIMP Enable(long index, DWORD flags);
    STDMETHODIMP Info( long lIndex,AM_MEDIA_TYPE **ppmt,DWORD *pdwFlags, LCID *plcid, DWORD *pdwGroup, WCHAR **ppszName, IUnknown **ppObject, IUnknown **ppUnk);
public:
	// IFileSourceFilter
	STDMETHODIMP    Load(LPCOLESTR pszFileName,const AM_MEDIA_TYPE *pmt);
	STDMETHODIMP    GetCurFile(LPOLESTR * ppszFileName,AM_MEDIA_TYPE *pmt);
	STDMETHODIMP    GetDuration(REFERENCE_TIME *dur);
	double		      GetStartTime();
	bool            IsSeeking();
  bool            IsFilterRunning();
	CDeMultiplexer& GetDemultiplexer();
	void            Seek(CRefTime& seekTime);
  void            SeekDone(CRefTime& refTime);
  void            SeekStart();
	double          UpdateDuration();
  CAudioPin*      GetAudioPin();
  CVideoPin*      GetVideoPin();
  CSubtitlePin*   GetSubtitlePin();
  bool            IsTimeShifting();
  
  CRefTime        Compensation;
protected:
  void ThreadProc();
private:
  void SetDuration();
  HRESULT AddGraphToRot(IUnknown *pUnkGraph) ;
  void    RemoveGraphFromRot();
	CAudioPin*	    m_pAudioPin;;
	CVideoPin*	    m_pVideoPin;
	CSubtitlePin*	  m_pSubtitlePin;
	WCHAR           m_fileName[1024];
	CCritSec        m_section;
	CCritSec        m_CritSecDuration;
	FileReader*     m_fileReader;
	FileReader*     m_fileDuration;
  CTsDuration     m_duration;
  CBaseReferenceClock* m_referenceClock;
	CDeMultiplexer  m_demultiplexer;
  bool            m_bSeeking;
  DWORD           m_dwGraphRegister;

  CRTSPClient     m_rtspClient;
  CMemoryBuffer   m_buffer;
  DWORD           m_tickCount;
  CRefTime        m_seekTime;
  bool            m_bNeedSeeking;
  bool            m_bTimeShifting;
};

