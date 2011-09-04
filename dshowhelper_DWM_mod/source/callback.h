// Copyright (C) 2005-2010 Team MediaPortal
// http://www.team-mediaportal.com
// 
// MediaPortal is free software: you can redistribute it and/or modify
// it under the terms of the GNU General Public License as published by
// the Free Software Foundation, either version 2 of the License, or
// (at your option) any later version.
// 
// MediaPortal is distributed in the hope that it will be useful,
// but WITHOUT ANY WARRANTY; without even the implied warranty of
// MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the
// GNU General Public License for more details.
// 
// You should have received a copy of the GNU General Public License
// along with MediaPortal. If not, see <http://www.gnu.org/licenses/>.

#pragma once

DECLARE_INTERFACE_(IVMR9Callback, IUnknown)
{
	STDMETHOD(PresentImage)  (THIS_ INT16 cx, INT16 cy, INT16 arx, INT16 ary, DWORD pTexture, DWORD pSurface)PURE;
	STDMETHOD(SetSampleTime)(REFERENCE_TIME nsSampleTime)PURE;
};
