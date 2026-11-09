// SPDX-License-Identifier: AGPL-3.0-or-later
namespace GearPdf.Core;
public static class PageEditing {
    // Marks are stored back to front; the panel presents the reverse order.
    public static bool MoveLayer(Page page,Guid id,int target){
        int from=page.Marks.FindIndex(m=>m.Id==id);
        if(from<0 || page.Marks.Count<2)return false;
        target=Math.Clamp(target,0,page.Marks.Count-1);if(target==from)return false;
        var mark=page.Marks[from];page.Marks.RemoveAt(from);page.Marks.Insert(target,mark);return true;
    }
    public static int DeletePage(Document document,int index){
        if(document.Pages.Count<=1)return 0;
        document.Pages.RemoveAt(index);return Math.Min(index,document.Pages.Count-1);
    }
    public static int MovePage(Document document,int index,int direction){
        int target=Math.Clamp(index+direction,0,document.Pages.Count-1);
        var page=document.Pages[index];document.Pages.RemoveAt(index);document.Pages.Insert(target,page);return target;
    }
    public static bool CanEdit(Mark mark)=>mark.Visible && !mark.Locked;
}
