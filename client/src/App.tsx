import { useEffect } from "react";
import { RouterProvider } from "react-router-dom";
import { Toaster } from "sonner";
import { useUserStore } from "@/stores/userStore";
import { router } from "@/routes";

export default function App() {
  const initUser = useUserStore((state) => state.initUser);

  useEffect(() => {
    void initUser();
  }, [initUser]);

  return (
    <>
      <RouterProvider router={router} />
      <Toaster richColors position="top-right" />
    </>
  );
}
