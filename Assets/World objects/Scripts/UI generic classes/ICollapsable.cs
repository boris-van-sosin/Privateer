using System;

public interface ICollapsable
{
    void Open();
    void Close();

    event Action<ICollapsable> onOpened;
    event Action<ICollapsable> onClosed;
}
